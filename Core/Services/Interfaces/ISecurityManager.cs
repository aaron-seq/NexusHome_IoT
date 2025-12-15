namespace NexusHome.IoT.Core.Services.Interfaces;

public interface ISecurityManager
{
    Task<bool> ValidateDeviceAccessAsync(string deviceId, string token);
    Task MonitorSecurityEventsAsync();
}
