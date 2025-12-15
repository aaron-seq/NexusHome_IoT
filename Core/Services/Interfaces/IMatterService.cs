using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IMatterService
{
    Task StartDiscoveryAsync();
    Task StopDiscoveryAsync();
    Task CommissionDeviceAsync(string payload);
    Task<bool> ConnectToDeviceAsync(string deviceId);
    Task SendCommandAsync(string deviceId, string command, object payload);
    Task<object> ReadAttributeAsync(string deviceId, string clusterId, string attributeId);
    Task WriteAttributeAsync(string deviceId, string clusterId, string attributeId, object value);
    Task SubscribeToEventsAsync(string deviceId);
}
