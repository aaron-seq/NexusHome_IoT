using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Core.Services;

/// <summary>
/// The "Universal Bridge" acts as the central orchestrator.
/// It decouples the UI from specific protocols (MQTT, Matter, Zigbee).
/// Flow: UI -> Bridge -> Shadow (Desired) -> Adapter -> Device.
/// </summary>
public class UniversalDeviceBridge
{
    private readonly ILogger<UniversalDeviceBridge> _logger;
    private readonly DeviceShadowService _shadowService;
    private readonly IEnumerable<IDeviceAdapter> _adapters;

    public UniversalDeviceBridge(
        ILogger<UniversalDeviceBridge> logger,
        DeviceShadowService shadowService,
        IEnumerable<IDeviceAdapter> adapters)
    {
        _logger = logger;
        _shadowService = shadowService;
        _adapters = adapters;
    }

    public async Task ExecuteCommandAsync(string deviceId, string commandName, Dictionary<string, object>? parameters = null)
    {
        _logger.LogInformation("Bridge received command {Command} for {DeviceId}", commandName, deviceId);

        // 1. Update Shadow "Desired" State (Intent)
        var desiredParams = parameters ?? new Dictionary<string, object>();
        // Map simple commands to state properties
        if (commandName.Equals("TurnOn", StringComparison.OrdinalIgnoreCase)) desiredParams["state"] = "ON";
        if (commandName.Equals("TurnOff", StringComparison.OrdinalIgnoreCase)) desiredParams["state"] = "OFF";
        
        await _shadowService.UpdateDesiredStateAsync(deviceId, desiredParams);

        // 2. Route to appropriate Adapter
        // In a real system, we'd lookup which adapter owns this device.
        // For now, we broadcast to all (simplification) or check prefix.
        
        var adapter = GetAdapterForDevice(deviceId);
        if (adapter != null)
        {
            var command = new DeviceCommand 
            { 
                CommandName = commandName, 
                Parameters = parameters ?? new Dictionary<string, object>() 
            };
            
            await adapter.ExecuteCommandAsync(deviceId, command);
        }
        else
        {
            _logger.LogWarning("No adapter found for device {DeviceId}", deviceId);
        }
    }

    private IDeviceAdapter? GetAdapterForDevice(string deviceId)
    {
        // Simple heuristic for Phase 3
        if (deviceId.StartsWith("mqtt-")) return _adapters.FirstOrDefault(a => a.ProtocolName == "MQTT");
        if (deviceId.StartsWith("matter-")) return _adapters.FirstOrDefault(a => a.ProtocolName == "Matter");
        return _adapters.FirstOrDefault(); // Fallback
    }
}
