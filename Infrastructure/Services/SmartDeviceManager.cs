using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Data;
using System.Text.Json;

namespace NexusHome.IoT.Infrastructure.Services;

public class SmartDeviceManager : ISmartDeviceManager
{
    private readonly SmartHomeDbContext _context;
    private readonly ILogger<SmartDeviceManager> _logger;
    private readonly IMqttClientService _mqttService;

    public SmartDeviceManager(
        SmartHomeDbContext context,
        ILogger<SmartDeviceManager> logger,
        IMqttClientService mqttService)
    {
        _context = context;
        _logger = logger;
        _mqttService = mqttService;
    }

    public async Task<IEnumerable<SmartHomeDevice>> GetAllDevicesAsync()
    {
        return await _context.SmartDevices
            .Where(d => d.IsActive)
            .OrderBy(d => d.DeviceFriendlyName)
            .ToListAsync();
    }

    public async Task<SmartHomeDevice?> GetDeviceByIdAsync(int deviceId)
    {
        return await _context.SmartDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.IsActive);
    }

    public async Task<SmartHomeDevice?> GetDeviceByIdAsync(string deviceId)
    {
        return await _context.SmartDevices
            .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == deviceId && d.IsActive);
    }
    
    // Overload for string identifier if needed, or stick to Int ID for internal, String for external
    // Interface defines Int currently.

    public async Task<SmartHomeDevice> AddDeviceAsync(SmartHomeDevice device)
    {
        device.CreatedAt = DateTime.UtcNow;
        device.IsActive = true;
        
        _context.SmartDevices.Add(device);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} ({Name}) added successfully", device.UniqueDeviceIdentifier, device.DeviceFriendlyName);
        
        // Publish device addition event
        await _mqttService.PublishAsync($"nexushome/devices/{device.UniqueDeviceIdentifier}/status", 
            JsonSerializer.Serialize(new { Status = "Added", Timestamp = DateTime.UtcNow }));
        
        return device;
    }

    public async Task<SmartHomeDevice> UpdateDeviceAsync(SmartHomeDevice device)
    {
        var existingDevice = await _context.SmartDevices
            .FirstOrDefaultAsync(d => d.Id == device.Id);
        
        if (existingDevice == null)
            throw new ArgumentException($"Device {device.Id} not found");
        
        existingDevice.DeviceFriendlyName = device.DeviceFriendlyName;
        existingDevice.PhysicalLocation = device.PhysicalLocation;
        existingDevice.RoomAssignment = device.RoomAssignment;
        existingDevice.DeviceConfigurationJson = device.DeviceConfigurationJson;
        existingDevice.AdditionalMetadataJson = device.AdditionalMetadataJson;
        existingDevice.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} updated successfully", device.UniqueDeviceIdentifier);
        
        return existingDevice;
    }

    public async Task<bool> DeleteDeviceAsync(int deviceId)
    {
        var device = await _context.SmartDevices.FindAsync(deviceId);
        
        if (device == null)
            return false;
        
        device.IsActive = false;
        device.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} marked as inactive", deviceId);
        
        return true;
    }

    public async Task<bool> ToggleDeviceAsync(int deviceId)
    {
        var device = await GetDeviceByIdAsync(deviceId);
        if (device == null)
            return false;
        
        // Toggle device state via MQTT command
        var command = new { Command = "toggle", Timestamp = DateTime.UtcNow };
        await _mqttService.PublishAsync($"nexushome/commands/{device.UniqueDeviceIdentifier}", 
            JsonSerializer.Serialize(command));
        
        _logger.LogInformation("Toggle command sent to device {DeviceId}", device.UniqueDeviceIdentifier);
        
        return true;
    }

    public async Task ProcessTelemetryDataAsync(int deviceId, object telemetryData)
    {
        var device = await GetDeviceByIdAsync(deviceId);
        if (device == null)
        {
            _logger.LogWarning("Telemetry received for unknown device ID {DeviceId}", deviceId);
            return;
        }
        
        device.LastCommunicationTime = DateTime.UtcNow;
        device.IsCurrentlyOnline = true;
        
        // Handle telemetry data (assuming dictionary or json element)
        if (telemetryData is JsonElement json)
        {
             // Try to extract power
             if (json.TryGetProperty("power", out var powerProp))
             {
                 decimal power = powerProp.GetDecimal();
                 device.CurrentPowerConsumption = power;
                 
                 _context.EnergyConsumptions.Add(new DeviceEnergyConsumption
                 {
                     SmartHomeDeviceId = device.Id,
                     PowerConsumptionKilowattHours = power / 1000m,
                     MeasurementTimestamp = DateTime.UtcNow,
                     VoltageReading = 220, // Default or extract
                     CurrentReading = power / 220,
                     EnergySource = EnergySourceType.ElectricGrid,
                     CalculatedCostAmount = (power / 1000m) * 0.15m 
                 });
             }
        }
        
        await _context.SaveChangesAsync();
        _logger.LogDebug("Telemetry processed for device {DeviceId}", device.UniqueDeviceIdentifier);
    }
}
