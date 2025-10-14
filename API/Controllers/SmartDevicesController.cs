using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexusHome.IoT.Application.DTOs;
using NexusHome.IoT.Core.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace NexusHome.IoT.API.Controllers;

/// <summary>
/// RESTful API controller for managing smart home devices
/// Provides comprehensive CRUD operations, device control, and telemetry management
/// Supports real-time device communication and status monitoring
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("StandardApiLimiter")]
[Produces("application/json")]
[SwaggerTag("Smart device management including registration, configuration, control, and monitoring")]
public class SmartDevicesController : ControllerBase
{
    private readonly ISmartDeviceManager _deviceManager;
    private readonly IMqttClientService _mqttClientService;
    private readonly IEnergyConsumptionAnalyzer _energyAnalyzer;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<SmartDevicesController> _logger;

    public SmartDevicesController(
        ISmartDeviceManager deviceManager,
        IMqttClientService mqttClientService,
        IEnergyConsumptionAnalyzer energyAnalyzer,
        INotificationDispatcher notificationDispatcher,
        ILogger<SmartDevicesController> logger)
    {
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _mqttClientService = mqttClientService ?? throw new ArgumentNullException(nameof(mqttClientService));
        _energyAnalyzer = energyAnalyzer ?? throw new ArgumentNullException(nameof(energyAnalyzer));
        _notificationDispatcher = notificationDispatcher ?? throw new ArgumentNullException(nameof(notificationDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all registered smart devices with optional filtering and pagination
    /// </summary>
    /// <param name="deviceCategory">Filter by device category (optional)</param>
    /// <param name="isOnlineOnly">Return only online devices (optional)</param>
    /// <param name="roomFilter">Filter by room assignment (optional)</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of devices per page (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of smart devices matching the specified criteria</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all smart devices",
        Description = "Retrieves a paginated list of all registered smart devices with optional filtering by category, online status, and room assignment."
    )]
    [SwaggerResponse((int)HttpStatusCode.OK, "Successfully retrieved device list", typeof(PaginatedResponse<SmartDeviceResponseDto>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "Internal server error occurred")]
    public async Task<ActionResult<PaginatedResponse<SmartDeviceResponseDto>>> GetAllDevicesAsync(
        [FromQuery] string? deviceCategory = null,
        [FromQuery] bool? isOnlineOnly = null,
        [FromQuery] string? roomFilter = null,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving smart devices with filters - Category: {Category}, OnlineOnly: {OnlineOnly}, Room: {Room}, Page: {Page}, Size: {Size}",
                deviceCategory, isOnlineOnly, roomFilter, pageNumber, pageSize);

            var deviceFilterCriteria = new DeviceFilterCriteria
            {
                DeviceCategory = deviceCategory,
                IsOnlineOnly = isOnlineOnly,
                RoomFilter = roomFilter,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var paginatedDevices = await _deviceManager.GetFilteredDevicesAsync(deviceFilterCriteria, cancellationToken);

            _logger.LogInformation("Successfully retrieved {DeviceCount} devices out of {TotalCount} total devices",
                paginatedDevices.Items.Count, paginatedDevices.TotalCount);

            return Ok(paginatedDevices);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while retrieving smart devices with filters");
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to retrieve devices", details = exception.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific smart device by its unique identifier
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="includeRecentTelemetry">Include recent telemetry data in response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Smart device details with optional telemetry data</returns>
    [HttpGet("{deviceId}")]
    [SwaggerOperation(
        Summary = "Get device by ID",
        Description = "Retrieves detailed information about a specific smart device, optionally including recent telemetry data."
    )]
    [SwaggerResponse((int)HttpStatusCode.OK, "Successfully retrieved device details", typeof(SmartDeviceResponseDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Device not found")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    public async Task<ActionResult<SmartDeviceResponseDto>> GetDeviceByIdAsync(
        [FromRoute, Required] string deviceId,
        [FromQuery] bool includeRecentTelemetry = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving device {DeviceId} with telemetry: {IncludeTelemetry}", 
                deviceId, includeRecentTelemetry);

            var deviceDetails = await _deviceManager.GetDeviceByIdAsync(deviceId, includeRecentTelemetry, cancellationToken);

            if (deviceDetails == null)
            {
                _logger.LogWarning("Device {DeviceId} not found", deviceId);
                return NotFound(new { error = "Device not found", deviceId });
            }

            _logger.LogInformation("Successfully retrieved device {DeviceId}: {DeviceName}", 
                deviceId, deviceDetails.FriendlyName);

            return Ok(deviceDetails);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while retrieving device {DeviceId}", deviceId);
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to retrieve device", deviceId, details = exception.Message });
        }
    }

    /// <summary>
    /// Registers a new smart device in the system
    /// </summary>
    /// <param name="deviceRequest">Device registration information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created device details with assigned system identifier</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Register new device",
        Description = "Registers a new smart device in the system with the provided configuration and settings."
    )]
    [SwaggerResponse((int)HttpStatusCode.Created, "Device successfully registered", typeof(SmartDeviceResponseDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Invalid device data provided")]
    [SwaggerResponse((int)HttpStatusCode.Conflict, "Device with same identifier already exists")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    public async Task<ActionResult<SmartDeviceResponseDto>> RegisterDeviceAsync(
        [FromBody, Required] SmartDeviceRequestDto deviceRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering new device: {DeviceId} - {DeviceName}", 
                deviceRequest.DeviceIdentifier, deviceRequest.FriendlyName);

            // Validate that device doesn't already exist
            var existingDevice = await _deviceManager.GetDeviceByIdAsync(deviceRequest.DeviceIdentifier, false, cancellationToken);
            if (existingDevice != null)
            {
                _logger.LogWarning("Attempt to register duplicate device {DeviceId}", deviceRequest.DeviceIdentifier);
                return Conflict(new { error = "Device with this identifier already exists", 
                    deviceId = deviceRequest.DeviceIdentifier });
            }

            var createdDevice = await _deviceManager.RegisterNewDeviceAsync(deviceRequest, cancellationToken);

            _logger.LogInformation("Successfully registered device {DeviceId}: {DeviceName}", 
                createdDevice.DeviceIdentifier, createdDevice.FriendlyName);

            // Send welcome message to device via MQTT
            await _mqttClientService.SendDeviceCommandAsync(
                createdDevice.DeviceIdentifier, 
                "device_registration_complete", 
                new { registrationTime = DateTime.UtcNow, systemId = createdDevice.Id },
                cancellationToken);

            return CreatedAtAction(
                nameof(GetDeviceByIdAsync), 
                new { deviceId = createdDevice.DeviceIdentifier }, 
                createdDevice);
        }
        catch (ValidationException validationException)
        {
            _logger.LogWarning(validationException, "Validation error while registering device {DeviceId}", 
                deviceRequest.DeviceIdentifier);
            return BadRequest(new { error = "Validation failed", details = validationException.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while registering device {DeviceId}", 
                deviceRequest.DeviceIdentifier);
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to register device", details = exception.Message });
        }
    }

    /// <summary>
    /// Updates configuration and settings for an existing smart device
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="deviceUpdateRequest">Updated device information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated device details</returns>
    [HttpPut("{deviceId}")]
    [SwaggerOperation(
        Summary = "Update device configuration",
        Description = "Updates the configuration and settings for an existing smart device."
    )]
    [SwaggerResponse((int)HttpStatusCode.OK, "Device successfully updated", typeof(SmartDeviceResponseDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Device not found")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Invalid update data provided")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    public async Task<ActionResult<SmartDeviceResponseDto>> UpdateDeviceAsync(
        [FromRoute, Required] string deviceId,
        [FromBody, Required] SmartDeviceRequestDto deviceUpdateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating device {DeviceId} with new configuration", deviceId);

            var updatedDevice = await _deviceManager.UpdateDeviceConfigurationAsync(deviceId, deviceUpdateRequest, cancellationToken);

            if (updatedDevice == null)
            {
                _logger.LogWarning("Device {DeviceId} not found for update", deviceId);
                return NotFound(new { error = "Device not found", deviceId });
            }

            _logger.LogInformation("Successfully updated device {DeviceId}: {DeviceName}", 
                deviceId, updatedDevice.FriendlyName);

            // Notify device of configuration update via MQTT
            await _mqttClientService.SendDeviceCommandAsync(
                deviceId, 
                "configuration_updated", 
                new { updateTime = DateTime.UtcNow, configVersion = updatedDevice.LastUpdatedTimestamp },
                cancellationToken);

            return Ok(updatedDevice);
        }
        catch (ValidationException validationException)
        {
            _logger.LogWarning(validationException, "Validation error while updating device {DeviceId}", deviceId);
            return BadRequest(new { error = "Validation failed", details = validationException.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while updating device {DeviceId}", deviceId);
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to update device", details = exception.Message });
        }
    }

    /// <summary>
    /// Removes a smart device from the system
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="forceDelete">Force deletion even if device is online</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{deviceId}")]
    [SwaggerOperation(
        Summary = "Delete device",
        Description = "Removes a smart device from the system. Optionally force deletion even if device is currently online."
    )]
    [SwaggerResponse((int)HttpStatusCode.NoContent, "Device successfully deleted")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Device not found")]
    [SwaggerResponse((int)HttpStatusCode.Conflict, "Cannot delete online device without force flag")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    public async Task<IActionResult> DeleteDeviceAsync(
        [FromRoute, Required] string deviceId,
        [FromQuery] bool forceDelete = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting device {DeviceId} with force: {ForceDelete}", deviceId, forceDelete);

            var deviceDetails = await _deviceManager.GetDeviceByIdAsync(deviceId, false, cancellationToken);
            if (deviceDetails == null)
            {
                _logger.LogWarning("Device {DeviceId} not found for deletion", deviceId);
                return NotFound(new { error = "Device not found", deviceId });
            }

            // Check if device is online and force flag is not set
            if (deviceDetails.IsOnlineAndReachable && !forceDelete)
            {
                _logger.LogWarning("Attempt to delete online device {DeviceId} without force flag", deviceId);
                return Conflict(new { 
                    error = "Cannot delete online device", 
                    deviceId, 
                    suggestion = "Use forceDelete=true to override this protection" 
                });
            }

            // Send shutdown command to device before deletion
            if (deviceDetails.IsOnlineAndReachable)
            {
                await _mqttClientService.SendDeviceCommandAsync(
                    deviceId, 
                    "prepare_for_deletion", 
                    new { deletionTime = DateTime.UtcNow, gracePeriodSeconds = 30 },
                    cancellationToken);

                // Wait a moment for device to process shutdown command
                await Task.Delay(2000, cancellationToken);
            }

            var deletionSuccessful = await _deviceManager.DeleteDeviceAsync(deviceId, cancellationToken);
            
            if (!deletionSuccessful)
            {
                _logger.LogError("Failed to delete device {DeviceId}", deviceId);
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { error = "Failed to delete device", deviceId });
            }

            _logger.LogInformation("Successfully deleted device {DeviceId}: {DeviceName}", 
                deviceId, deviceDetails.FriendlyName);

            return NoContent();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while deleting device {DeviceId}", deviceId);
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to delete device", details = exception.Message });
        }
    }

    /// <summary>
    /// Sends a control command to a smart device
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="commandRequest">Command details and parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Command execution status</returns>
    [HttpPost("{deviceId}/commands")]
    [EnableRateLimiting("DeviceTelemetryLimiter")]
    [SwaggerOperation(
        Summary = "Send device command",
        Description = "Sends a control command to a specific smart device, such as power on/off, temperature adjustment, or custom actions."
    )]
    [SwaggerResponse((int)HttpStatusCode.Accepted, "Command successfully queued for execution")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Device not found")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Invalid command data")]
    [SwaggerResponse((int)HttpStatusCode.ServiceUnavailable, "Device is offline or unreachable")]
    public async Task<IActionResult> SendDeviceCommandAsync(
        [FromRoute, Required] string deviceId,
        [FromBody, Required] DeviceCommandRequestDto commandRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending command {CommandType} to device {DeviceId} with priority {Priority}",
                commandRequest.CommandType, deviceId, commandRequest.PriorityLevel);

            // Verify device exists and is reachable
            var deviceDetails = await _deviceManager.GetDeviceByIdAsync(deviceId, false, cancellationToken);
            if (deviceDetails == null)
            {
                _logger.LogWarning("Command target device {DeviceId} not found", deviceId);
                return NotFound(new { error = "Device not found", deviceId });
            }

            if (!deviceDetails.IsOnlineAndReachable)
            {
                _logger.LogWarning("Command sent to offline device {DeviceId}", deviceId);
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, 
                    new { error = "Device is offline or unreachable", deviceId });
            }

            // Execute command via device manager
            var commandResult = await _deviceManager.ExecuteDeviceCommandAsync(deviceId, commandRequest, cancellationToken);

            _logger.LogInformation("Command {CommandType} queued for device {DeviceId} with execution ID {ExecutionId}",
                commandRequest.CommandType, deviceId, commandResult.ExecutionId);

            return Accepted(new { 
                executionId = commandResult.ExecutionId,
                deviceId = deviceId,
                commandType = commandRequest.CommandType,
                status = "queued",
                estimatedExecutionTime = commandResult.EstimatedExecutionTime
            });
        }
        catch (ValidationException validationException)
        {
            _logger.LogWarning(validationException, "Validation error for command to device {DeviceId}", deviceId);
            return BadRequest(new { error = "Command validation failed", details = validationException.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while sending command to device {DeviceId}", deviceId);
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to execute device command", details = exception.Message });
        }
    }

    /// <summary>
    /// Submits telemetry data from a smart device
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="telemetryData">Device telemetry and sensor readings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Telemetry processing confirmation</returns>
    [HttpPost("{deviceId}/telemetry")]
    [EnableRateLimiting("DeviceTelemetryLimiter")]
    [SwaggerOperation(
        Summary = "Submit device telemetry",
        Description = "Submits telemetry data from a smart device including sensor readings, power consumption, and operational status."
    )]
    [SwaggerResponse((int)HttpStatusCode.Accepted, "Telemetry data successfully received and queued for processing")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Device not found")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Invalid telemetry data format")]
    public async Task<IActionResult> SubmitDeviceTelemetryAsync(
        [FromRoute, Required] string deviceId,
        [FromBody, Required] DeviceTelemetrySubmissionDto telemetryData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Received telemetry from device {DeviceId} with {SensorCount} sensor readings",
                deviceId, telemetryData.SensorReadings.Count);

            // Validate device exists
            var deviceExists = await _deviceManager.DeviceExistsAsync(deviceId, cancellationToken);
            if (!deviceExists)
            {
                _logger.LogWarning("Telemetry submitted for unknown device {DeviceId}", deviceId);
                return NotFound(new { error = "Device not found", deviceId });
            }

            // Process telemetry data
            var processingResult = await _deviceManager.ProcessDeviceTelemetryAsync(telemetryData, cancellationToken);

            // Update device last-seen timestamp
            await _deviceManager.UpdateDeviceLastSeenAsync(deviceId, cancellationToken);

            _logger.LogDebug("Successfully processed telemetry for device {DeviceId}", deviceId);

            return Accepted(new {
                deviceId = deviceId,
                processingId = processingResult.ProcessingId,
                timestamp = processingResult.ProcessedAt,
                status = "processed",
                anomaliesDetected = processingResult.AnomaliesDetected
            });
        }
        catch (ValidationException validationException)
        {
            _logger.LogWarning(validationException, "Validation error for telemetry from device {DeviceId}", deviceId);
            return BadRequest(new { error = "Telemetry validation failed", details = validationException.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while processing telemetry from device {DeviceId}", deviceId);
            return StatusCode((int)HttpStatusCode.InternalServerError, 
                new { error = "Failed to process telemetry data", details = exception.Message });
        }
    }
}

/// <summary>
/// Request model for device filtering and pagination
/// </summary>
public class DeviceFilterCriteria
{
    public string? DeviceCategory { get; set; }
    public bool? IsOnlineOnly { get; set; }
    public string? RoomFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Generic paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
