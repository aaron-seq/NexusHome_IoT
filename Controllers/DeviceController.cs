using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Application.DTOs;

namespace NexusHome.IoT.Controllers;

/// <summary>
/// Controller for managing smart home devices and their operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UserAccess")]
[Produces("application/json")]
public class DeviceController : ControllerBase
{
    private readonly SmartHomeDbContext _context;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(SmartHomeDbContext context, ILogger<DeviceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all smart home devices
    /// </summary>
    /// <returns>List of all registered devices</returns>
    /// <response code="200">Returns the list of devices</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAllDevices()
    {
        try
        {
            var devices = await _context.SmartDevices
                .AsNoTracking()
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    DeviceId = d.UniqueDeviceIdentifier,
                    Name = d.DeviceFriendlyName,
                    Type = d.DeviceType.ToString(),
                    Location = d.PhysicalLocation,
                    IsOnline = d.IsCurrentlyOnline,
                    IsActive = d.IsActive,
                    PowerConsumption = d.CurrentPowerConsumption,
                    Manufacturer = d.ManufacturerName,
                    Model = d.ModelNumber,
                    FirmwareVersion = d.FirmwareVersion,
                    LastSeen = d.LastCommunicationTime
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} devices", devices.Count);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices");
            return StatusCode(500, new { error = "Failed to retrieve devices" });
        }
    }

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <returns>Device details</returns>
    /// <response code="200">Returns the device</response>
    /// <response code="404">Device not found</response>
    [HttpGet("{deviceId}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetDevice(string deviceId)
    {
        try
        {
            var device = await _context.SmartDevices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == deviceId);

            if (device == null)
            {
                _logger.LogWarning("Device {DeviceId} not found", deviceId);
                return NotFound(new { error = $"Device {deviceId} not found" });
            }

            var deviceDto = new DeviceDto
            {
                Id = device.Id,
                DeviceId = device.UniqueDeviceIdentifier,
                Name = device.DeviceFriendlyName,
                Type = device.DeviceType.ToString(),
                Location = device.PhysicalLocation,
                IsOnline = device.IsCurrentlyOnline,
                IsActive = device.IsActive,
                PowerConsumption = device.CurrentPowerConsumption,
                Manufacturer = device.ManufacturerName,
                Model = device.ModelNumber,
                FirmwareVersion = device.FirmwareVersion,
                LastSeen = device.LastCommunicationTime
            };

            return Ok(deviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to retrieve device" });
        }
    }

    /// <summary>
    /// Toggle device state (on/off)
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <returns>Success message</returns>
    /// <response code="200">Device toggled successfully</response>
    /// <response code="404">Device not found</response>
    [HttpPost("{deviceId}/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleDevice(string deviceId)
    {
        try
        {
            var device = await _context.SmartDevices
                .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == deviceId);

            if (device == null)
            {
                return NotFound(new { error = $"Device {deviceId} not found" });
            }

            device.IsActive = !device.IsActive;
            device.LastCommunicationTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceId} toggled to {State}", deviceId, device.IsActive ? "ON" : "OFF");

            return Ok(new 
            { 
                message = $"Device {deviceId} toggled successfully",
                isActive = device.IsActive,
                deviceId = deviceId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to toggle device" });
        }
    }

    /// <summary>
    /// Submit device telemetry data
    /// </summary>
    /// <param name="request">Telemetry data</param>
    /// <returns>Acceptance confirmation</returns>
    /// <response code="202">Telemetry accepted for processing</response>
    /// <response code="400">Invalid telemetry data</response>
    [HttpPost("telemetry")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitTelemetry([FromBody] DeviceTelemetryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var device = await _context.SmartDevices
                .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == request.DeviceId);

            if (device == null)
            {
                return BadRequest(new { error = $"Device {request.DeviceId} not found" });
            }

            // Update device status
            device.LastCommunicationTime = DateTime.UtcNow;
            device.IsCurrentlyOnline = true;
            device.CurrentPowerConsumption = request.PowerConsumption ?? device.CurrentPowerConsumption;

            // Create energy consumption record
            var energyRecord = new DeviceEnergyConsumption
            {
                SmartHomeDeviceId = device.Id,
                PowerConsumptionKilowattHours = (request.PowerConsumption ?? 0) / 1000m,
                VoltageReading = request.Voltage ?? 220m,
                CurrentReading = request.Current ?? 0m,
                FrequencyReading = request.Frequency ?? 50m,
                PowerFactorReading = request.PowerFactor ?? 0.95m,
                MeasurementTimestamp = DateTime.UtcNow,
                CostEstimate = ((request.PowerConsumption ?? 0) / 1000m) * 0.12m
            };

            _context.EnergyConsumptions.Add(energyRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Telemetry received from device {DeviceId}", request.DeviceId);

            return Accepted(new { message = "Telemetry data accepted", deviceId = request.DeviceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing telemetry from device {DeviceId}", request.DeviceId);
            return StatusCode(500, new { error = "Failed to process telemetry" });
        }
    }

    /// <summary>
    /// Get energy consumption data for a specific device
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="from">Start date (optional, defaults to 7 days ago)</param>
    /// <param name="to">End date (optional, defaults to now)</param>
    /// <returns>Energy consumption data points</returns>
    /// <response code="200">Returns energy consumption data</response>
    /// <response code="404">Device not found</response>
    [HttpGet("{deviceId}/energy")]
    [ProducesResponseType(typeof(IEnumerable<EnergyConsumptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnergyConsumptionDto>>> GetDeviceEnergy(
        string deviceId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var device = await _context.SmartDevices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == deviceId);

            if (device == null)
            {
                return NotFound(new { error = $"Device {deviceId} not found" });
            }

            var startDate = from ?? DateTime.UtcNow.AddDays(-7);
            var endDate = to ?? DateTime.UtcNow;

            var energyData = await _context.EnergyConsumptions
                .AsNoTracking()
                .Where(e => e.SmartHomeDeviceId == device.Id && 
                           e.MeasurementTimestamp >= startDate && 
                           e.MeasurementTimestamp <= endDate)
                .OrderBy(e => e.MeasurementTimestamp)
                .Select(e => new EnergyConsumptionDto
                {
                    Timestamp = e.MeasurementTimestamp,
                    PowerConsumptionKwh = e.PowerConsumptionKilowattHours,
                    Voltage = e.VoltageReading,
                    Current = e.CurrentReading,
                    Frequency = e.FrequencyReading,
                    PowerFactor = e.PowerFactorReading,
                    CostEstimate = e.CostEstimate
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} energy records for device {DeviceId}", 
                energyData.Count, deviceId);

            return Ok(energyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving energy data for device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to retrieve energy data" });
        }
    }
}
