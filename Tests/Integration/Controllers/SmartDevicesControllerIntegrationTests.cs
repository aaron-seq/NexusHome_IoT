using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusHome.IoT.Application.DTOs;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;

namespace NexusHome.IoT.Tests.Integration.Controllers;

/// <summary>
/// Comprehensive integration tests for SmartDevicesController
/// Tests complete request-response cycles using WebApplicationFactory
/// Validates API contracts, authentication, validation, and database interactions
/// </summary>
public class SmartDevicesControllerIntegrationTests : IClassFixture<SmartDeviceWebApplicationFactory>, IDisposable
{
    private readonly SmartDeviceWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IServiceScope _serviceScope;
    private readonly SmartHomeDbContext _databaseContext;

    public SmartDevicesControllerIntegrationTests(
        SmartDeviceWebApplicationFactory factory,
        ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
        _serviceScope = factory.Services.CreateScope();
        _databaseContext = _serviceScope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        // Ensure clean database state for each test
        SeedTestDatabase().GetAwaiter().GetResult();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task GetAllDevices_WithNoFilters_ShouldReturnPaginatedDeviceList()
    {
        // Arrange
        await SeedMultipleTestDevices();
        var requestUri = "/api/v1/smartdevices?pageSize=10";

        // Act
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();
        var paginatedResult = JsonSerializer.Deserialize<PaginatedResponse<SmartDeviceResponseDto>>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paginatedResult.Should().NotBeNull();
        paginatedResult!.Items.Should().HaveCountGreaterThan(0);
        paginatedResult.TotalCount.Should().BeGreaterThan(0);
        paginatedResult.PageNumber.Should().Be(1);
        paginatedResult.PageSize.Should().Be(10);

        // Validate response structure
        var firstDevice = paginatedResult.Items.First();
        firstDevice.DeviceIdentifier.Should().NotBeNullOrEmpty();
        firstDevice.FriendlyName.Should().NotBeNullOrEmpty();
        firstDevice.DeviceCategory.Should().NotBeNullOrEmpty();
        firstDevice.ManufacturerName.Should().NotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Retrieved {paginatedResult.Items.Count} devices out of {paginatedResult.TotalCount} total");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task GetAllDevices_WithCategoryFilter_ShouldReturnFilteredDevices()
    {
        // Arrange
        await SeedMultipleTestDevices();
        var requestUri = "/api/v1/smartdevices?deviceCategory=lighting&pageSize=20";

        // Act
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();
        var paginatedResult = JsonSerializer.Deserialize<PaginatedResponse<SmartDeviceResponseDto>>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paginatedResult.Should().NotBeNull();
        paginatedResult!.Items.Should().OnlyContain(device => device.DeviceCategory.ToLowerInvariant() == "lighting");

        _testOutputHelper.WriteLine($"Filtered results: {paginatedResult.Items.Count} lighting devices found");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task GetDeviceById_WithValidId_ShouldReturnDeviceDetails()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}";

        // Act
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();
        var deviceResponse = JsonSerializer.Deserialize<SmartDeviceResponseDto>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        deviceResponse.Should().NotBeNull();
        deviceResponse!.DeviceIdentifier.Should().Be(testDevice.UniqueDeviceIdentifier);
        deviceResponse.FriendlyName.Should().Be(testDevice.DeviceFriendlyName);
        deviceResponse.DeviceCategory.Should().Be(testDevice.DeviceType.ToString());
        deviceResponse.ManufacturerName.Should().Be(testDevice.ManufacturerName);
        deviceResponse.IsOnlineAndReachable.Should().Be(testDevice.IsCurrentlyOnline);

        _testOutputHelper.WriteLine($"Retrieved device: {deviceResponse.FriendlyName} ({deviceResponse.DeviceIdentifier})");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task GetDeviceById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentDeviceId = "non-existent-device-12345";
        var requestUri = $"/api/v1/smartdevices/{nonExistentDeviceId}";

        // Act
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        responseContent.Should().Contain("Device not found");
        responseContent.Should().Contain(nonExistentDeviceId);

        _testOutputHelper.WriteLine($"Correctly returned 404 for non-existent device: {nonExistentDeviceId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task RegisterDevice_WithValidData_ShouldCreateDeviceAndReturnCreated()
    {
        // Arrange
        var deviceRequest = CreateValidDeviceRequest("integration-test-device-001");
        var requestUri = "/api/v1/smartdevices";
        var jsonContent = JsonSerializer.Serialize(deviceRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdDevice = JsonSerializer.Deserialize<SmartDeviceResponseDto>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        createdDevice.Should().NotBeNull();
        createdDevice!.DeviceIdentifier.Should().Be(deviceRequest.DeviceIdentifier);
        createdDevice.FriendlyName.Should().Be(deviceRequest.FriendlyName);
        createdDevice.DeviceCategory.Should().Be(deviceRequest.DeviceCategory);
        createdDevice.ManufacturerName.Should().Be(deviceRequest.ManufacturerName);
        createdDevice.Id.Should().BeGreaterThan(0);
        createdDevice.DeviceRegistrationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(deviceRequest.DeviceIdentifier);

        // Verify device was actually saved to database
        var savedDevice = await _databaseContext.SmartDevices
            .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == deviceRequest.DeviceIdentifier);
        savedDevice.Should().NotBeNull();

        _testOutputHelper.WriteLine($"Successfully created device: {createdDevice.FriendlyName} with ID {createdDevice.Id}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task RegisterDevice_WithDuplicateId_ShouldReturnConflict()
    {
        // Arrange
        var existingDevice = await SeedSingleTestDevice();
        var deviceRequest = CreateValidDeviceRequest(existingDevice.UniqueDeviceIdentifier);
        var requestUri = "/api/v1/smartdevices";
        var jsonContent = JsonSerializer.Serialize(deviceRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseContent.Should().Contain("Device with this identifier already exists");
        responseContent.Should().Contain(existingDevice.UniqueDeviceIdentifier);

        _testOutputHelper.WriteLine($"Correctly rejected duplicate device ID: {existingDevice.UniqueDeviceIdentifier}");
    }

    [Theory]
    [InlineData("", "Device identifier is required")]
    [InlineData("ab", "Device identifier must be between 3 and 100 characters")]
    [InlineData("device with spaces", "Device identifier must start and end with alphanumeric characters")]
    [InlineData("device@invalid", "Device identifier must start and end with alphanumeric characters")]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task RegisterDevice_WithInvalidDeviceId_ShouldReturnBadRequest(string invalidDeviceId, string expectedErrorMessage)
    {
        // Arrange
        var deviceRequest = CreateValidDeviceRequest(invalidDeviceId);
        var requestUri = "/api/v1/smartdevices";
        var jsonContent = JsonSerializer.Serialize(deviceRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseContent.Should().Contain("Validation failed");

        _testOutputHelper.WriteLine($"Validation correctly rejected invalid device ID '{invalidDeviceId}': {expectedErrorMessage}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task UpdateDevice_WithValidData_ShouldUpdateDeviceAndReturnOk()
    {
        // Arrange
        var existingDevice = await SeedSingleTestDevice();
        var updateRequest = CreateValidDeviceRequest(existingDevice.UniqueDeviceIdentifier);
        updateRequest.FriendlyName = "Updated Smart Light Pro Max";
        updateRequest.Description = "Updated device description with new features";
        updateRequest.PhysicalLocation = "Updated Master Bedroom";
        updateRequest.MaximumPowerRatingWatts = 75m;

        var requestUri = $"/api/v1/smartdevices/{existingDevice.UniqueDeviceIdentifier}";
        var jsonContent = JsonSerializer.Serialize(updateRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PutAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        var updatedDevice = JsonSerializer.Deserialize<SmartDeviceResponseDto>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedDevice.Should().NotBeNull();
        updatedDevice!.FriendlyName.Should().Be(updateRequest.FriendlyName);
        updatedDevice.Description.Should().Be(updateRequest.Description);
        updatedDevice.PhysicalLocation.Should().Be(updateRequest.PhysicalLocation);
        updatedDevice.MaximumPowerRatingWatts.Should().Be(updateRequest.MaximumPowerRatingWatts);
        updatedDevice.LastUpdatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify database was actually updated
        var databaseDevice = await _databaseContext.SmartDevices
            .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == existingDevice.UniqueDeviceIdentifier);
        databaseDevice.Should().NotBeNull();
        databaseDevice!.DeviceFriendlyName.Should().Be(updateRequest.FriendlyName);

        _testOutputHelper.WriteLine($"Successfully updated device: {updatedDevice.FriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task UpdateDevice_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentDeviceId = "non-existent-device-update-test";
        var updateRequest = CreateValidDeviceRequest(nonExistentDeviceId);
        var requestUri = $"/api/v1/smartdevices/{nonExistentDeviceId}";
        var jsonContent = JsonSerializer.Serialize(updateRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PutAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        responseContent.Should().Contain("Device not found");
        responseContent.Should().Contain(nonExistentDeviceId);

        _testOutputHelper.WriteLine($"Correctly returned 404 for non-existent device update: {nonExistentDeviceId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task DeleteDevice_WithOfflineDevice_ShouldDeleteAndReturnNoContent()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        // Ensure device is offline
        testDevice.IsCurrentlyOnline = false;
        _databaseContext.SmartDevices.Update(testDevice);
        await _databaseContext.SaveChangesAsync();

        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}";

        // Act
        var response = await _httpClient.DeleteAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify device was actually deleted from database
        var deletedDevice = await _databaseContext.SmartDevices
            .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == testDevice.UniqueDeviceIdentifier);
        deletedDevice.Should().BeNull();

        _testOutputHelper.WriteLine($"Successfully deleted offline device: {testDevice.DeviceFriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task DeleteDevice_WithOnlineDeviceWithoutForce_ShouldReturnConflict()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        // Ensure device is online
        testDevice.IsCurrentlyOnline = true;
        _databaseContext.SmartDevices.Update(testDevice);
        await _databaseContext.SaveChangesAsync();

        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}";

        // Act
        var response = await _httpClient.DeleteAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseContent.Should().Contain("Cannot delete online device");
        responseContent.Should().Contain("forceDelete=true");

        // Verify device was NOT deleted
        var stillExistingDevice = await _databaseContext.SmartDevices
            .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == testDevice.UniqueDeviceIdentifier);
        stillExistingDevice.Should().NotBeNull();

        _testOutputHelper.WriteLine($"Correctly prevented deletion of online device: {testDevice.DeviceFriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task DeleteDevice_WithOnlineDeviceWithForce_ShouldDeleteAndReturnNoContent()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        // Ensure device is online
        testDevice.IsCurrentlyOnline = true;
        _databaseContext.SmartDevices.Update(testDevice);
        await _databaseContext.SaveChangesAsync();

        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}?forceDelete=true";

        // Act
        var response = await _httpClient.DeleteAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify device was actually deleted from database
        var deletedDevice = await _databaseContext.SmartDevices
            .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == testDevice.UniqueDeviceIdentifier);
        deletedDevice.Should().BeNull();

        _testOutputHelper.WriteLine($"Successfully force-deleted online device: {testDevice.DeviceFriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task SendDeviceCommand_WithValidCommand_ShouldReturnAccepted()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        // Ensure device is online
        testDevice.IsCurrentlyOnline = true;
        _databaseContext.SmartDevices.Update(testDevice);
        await _databaseContext.SaveChangesAsync();

        var commandRequest = new DeviceCommandRequestDto
        {
            DeviceIdentifier = testDevice.UniqueDeviceIdentifier,
            CommandType = "power_toggle",
            CommandParameters = new Dictionary<string, object>
            {
                { "transition_time", 1000 },
                { "brightness_level", 75 }
            },
            PriorityLevel = "normal",
            TimeoutSeconds = 30,
            RequireConfirmation = true
        };

        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}/commands";
        var jsonContent = JsonSerializer.Serialize(commandRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        var commandResult = JsonSerializer.Deserialize<Dictionary<string, object>>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        commandResult.Should().NotBeNull();
        commandResult!.Should().ContainKey("executionId");
        commandResult.Should().ContainKey("deviceId");
        commandResult.Should().ContainKey("commandType");
        commandResult.Should().ContainKey("status");
        commandResult["status"].ToString().Should().Be("queued");
        commandResult["deviceId"].ToString().Should().Be(testDevice.UniqueDeviceIdentifier);
        commandResult["commandType"].ToString().Should().Be(commandRequest.CommandType);

        _testOutputHelper.WriteLine($"Successfully queued command {commandRequest.CommandType} for device {testDevice.DeviceFriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task SendDeviceCommand_WithOfflineDevice_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        // Ensure device is offline
        testDevice.IsCurrentlyOnline = false;
        _databaseContext.SmartDevices.Update(testDevice);
        await _databaseContext.SaveChangesAsync();

        var commandRequest = new DeviceCommandRequestDto
        {
            DeviceIdentifier = testDevice.UniqueDeviceIdentifier,
            CommandType = "power_on",
            PriorityLevel = "normal"
        };

        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}/commands";
        var jsonContent = JsonSerializer.Serialize(commandRequest, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        responseContent.Should().Contain("Device is offline or unreachable");
        responseContent.Should().Contain(testDevice.UniqueDeviceIdentifier);

        _testOutputHelper.WriteLine($"Correctly rejected command for offline device: {testDevice.DeviceFriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task SubmitDeviceTelemetry_WithValidData_ShouldReturnAccepted()
    {
        // Arrange
        var testDevice = await SeedSingleTestDevice();
        var telemetryData = new DeviceTelemetrySubmissionDto
        {
            DeviceIdentifier = testDevice.UniqueDeviceIdentifier,
            TelemetryTimestamp = DateTime.UtcNow.AddMinutes(-1),
            SensorReadings = new Dictionary<string, object>
            {
                { "temperature", 22.5 },
                { "humidity", 45.2 },
                { "light_level", 750 },
                { "motion_detected", false }
            },
            CurrentPowerConsumptionWatts = 25.75m,
            OperatingTemperatureCelsius = 35.2m,
            NetworkSignalStrength = 87,
            DeviceStatus = "normal_operation",
            DataQualityIndicator = "Good"
        };

        var requestUri = $"/api/v1/smartdevices/{testDevice.UniqueDeviceIdentifier}/telemetry";
        var jsonContent = JsonSerializer.Serialize(telemetryData, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();
        var processingResult = JsonSerializer.Deserialize<Dictionary<string, object>>(
            responseContent, GetJsonSerializerOptions());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        processingResult.Should().NotBeNull();
        processingResult!.Should().ContainKey("processingId");
        processingResult.Should().ContainKey("deviceId");
        processingResult.Should().ContainKey("timestamp");
        processingResult.Should().ContainKey("status");
        processingResult["status"].ToString().Should().Be("processed");
        processingResult["deviceId"].ToString().Should().Be(testDevice.UniqueDeviceIdentifier);

        _testOutputHelper.WriteLine($"Successfully processed telemetry for device {testDevice.DeviceFriendlyName}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "SmartDevicesController")]
    public async Task SubmitDeviceTelemetry_WithNonExistentDevice_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentDeviceId = "non-existent-telemetry-device";
        var telemetryData = new DeviceTelemetrySubmissionDto
        {
            DeviceIdentifier = nonExistentDeviceId,
            TelemetryTimestamp = DateTime.UtcNow.AddMinutes(-1),
            SensorReadings = new Dictionary<string, object> { { "test_sensor", 42.0 } },
            DataQualityIndicator = "Good"
        };

        var requestUri = $"/api/v1/smartdevices/{nonExistentDeviceId}/telemetry";
        var jsonContent = JsonSerializer.Serialize(telemetryData, GetJsonSerializerOptions());
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        responseContent.Should().Contain("Device not found");
        responseContent.Should().Contain(nonExistentDeviceId);

        _testOutputHelper.WriteLine($"Correctly rejected telemetry for non-existent device: {nonExistentDeviceId}");
    }

    // Helper methods for test setup and data creation
    private async Task SeedTestDatabase()
    {
        // Ensure database is created and clean
        await _databaseContext.Database.EnsureCreatedAsync();
        
        // Remove existing test data
        var existingDevices = _databaseContext.SmartDevices
            .Where(d => d.UniqueDeviceIdentifier.StartsWith("test-") || 
                       d.UniqueDeviceIdentifier.StartsWith("integration-test-"));
        _databaseContext.SmartDevices.RemoveRange(existingDevices);
        
        await _databaseContext.SaveChangesAsync();
    }

    private async Task<SmartDevice> SeedSingleTestDevice()
    {
        var testDevice = new SmartDevice
        {
            UniqueDeviceIdentifier = $"test-device-{Guid.NewGuid():N}"[..25],
            DeviceFriendlyName = "Test Smart Light",
            DeviceDescription = "Integration test smart light device",
            DeviceType = DeviceCategory.Lighting,
            ManufacturerName = "TestManufacturer",
            ModelNumber = "TM-001",
            FirmwareVersion = "1.2.3",
            CommunicationProtocol = "WiFi",
            PhysicalLocation = "Test Living Room",
            MaximumPowerRatingWatts = 60m,
            CurrentPowerConsumption = 25.5m,
            IsCurrentlyOnline = true,
            CurrentStatus = DeviceOperationalStatus.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastCommunicationTime = DateTime.UtcNow.AddMinutes(-5)
        };

        _databaseContext.SmartDevices.Add(testDevice);
        await _databaseContext.SaveChangesAsync();
        return testDevice;
    }

    private async Task SeedMultipleTestDevices()
    {
        var testDevices = new List<SmartDevice>
        {
            new SmartDevice
            {
                UniqueDeviceIdentifier = $"test-light-{Guid.NewGuid():N}"[..20],
                DeviceFriendlyName = "Test Smart Bulb 1",
                DeviceType = DeviceCategory.Lighting,
                ManufacturerName = "Philips",
                ModelNumber = "HUE-001",
                PhysicalLocation = "Living Room",
                MaximumPowerRatingWatts = 9m,
                IsCurrentlyOnline = true,
                CurrentStatus = DeviceOperationalStatus.Normal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastCommunicationTime = DateTime.UtcNow.AddMinutes(-2)
            },
            new SmartDevice
            {
                UniqueDeviceIdentifier = $"test-thermostat-{Guid.NewGuid():N}"[..25],
                DeviceFriendlyName = "Test Smart Thermostat",
                DeviceType = DeviceCategory.ClimateControl,
                ManufacturerName = "Nest",
                ModelNumber = "NST-002",
                PhysicalLocation = "Hallway",
                MaximumPowerRatingWatts = 200m,
                IsCurrentlyOnline = true,
                CurrentStatus = DeviceOperationalStatus.Normal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastCommunicationTime = DateTime.UtcNow.AddMinutes(-1)
            },
            new SmartDevice
            {
                UniqueDeviceIdentifier = $"test-camera-{Guid.NewGuid():N}"[..20],
                DeviceFriendlyName = "Test Security Camera",
                DeviceType = DeviceCategory.Security,
                ManufacturerName = "Ring",
                ModelNumber = "RNG-CAM-001",
                PhysicalLocation = "Front Door",
                MaximumPowerRatingWatts = 15m,
                IsCurrentlyOnline = false,
                CurrentStatus = DeviceOperationalStatus.Offline,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastCommunicationTime = DateTime.UtcNow.AddHours(-2)
            }
        };

        _databaseContext.SmartDevices.AddRange(testDevices);
        await _databaseContext.SaveChangesAsync();
    }

    private static SmartDeviceRequestDto CreateValidDeviceRequest(string deviceIdentifier)
    {
        return new SmartDeviceRequestDto
        {
            DeviceIdentifier = deviceIdentifier,
            FriendlyName = "Integration Test Smart Light",
            Description = "Test smart light device for integration testing",
            DeviceCategory = "lighting",
            ManufacturerName = "TestCorp",
            ModelNumber = "TC-SL-001",
            FirmwareVersion = "2.1.0",
            CommunicationProtocol = "WiFi",
            PhysicalLocation = "Test Bedroom",
            AssignedRoom = "Bedroom",
            MaximumPowerRatingWatts = 60m,
            NetworkConfiguration = new DeviceNetworkConfigurationDto
            {
                IpAddress = "192.168.1.100",
                MacAddress = "00:1A:2B:3C:4D:5E",
                UsesDynamicIpAssignment = true
            },
            MqttConfiguration = new DeviceMqttConfigurationDto
            {
                TopicPath = "nexushome/devices/test-light",
                QualityOfServiceLevel = 1,
                RetainMessages = false,
                HeartbeatIntervalSeconds = 60
            },
            AdditionalMetadata = new Dictionary<string, object>
            {
                { "test_flag", true },
                { "integration_test_version", "1.0" },
                { "created_by", "integration_test_suite" }
            }
        };
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _serviceScope?.Dispose();
    }
}

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// Configures test environment with in-memory database and test services
/// </summary>
public class SmartDeviceWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SmartHomeDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<SmartHomeDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Override logging for testing
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce log noise in tests
            });

            // Add any test-specific service overrides here
            // For example, mock external services like MQTT client, email service, etc.
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Response model for paginated API results
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
