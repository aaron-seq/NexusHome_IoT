using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Core.DTOs;

namespace NexusHome.IoT.Core.Services.Interfaces;

public interface ISmartDeviceManager
{
    Task<IEnumerable<SmartHomeDevice>> GetAllDevicesAsync();
    Task<SmartHomeDevice?> GetDeviceByIdAsync(int deviceId);
    Task<SmartHomeDevice?> GetDeviceByIdAsync(string deviceId);
    Task<SmartHomeDevice> AddDeviceAsync(SmartHomeDevice device);
    Task<SmartHomeDevice> UpdateDeviceAsync(SmartHomeDevice device);
    Task<bool> DeleteDeviceAsync(int deviceId);
    
    // Additional methods based on usage or implementation
    Task<bool> ToggleDeviceAsync(int deviceId); // Changed to int based on Domain
    Task ProcessTelemetryDataAsync(int deviceId, object telemetryData);
}
