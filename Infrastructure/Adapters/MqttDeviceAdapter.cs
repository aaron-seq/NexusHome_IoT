using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Core.Services; // Added for DeviceShadowService

namespace NexusHome.IoT.Infrastructure.Adapters;

/// <summary>
/// Device adapter for MQTT-based devices.
/// Provides backward compatibility with existing MQTT infrastructure.
/// </summary>
public class MqttDeviceAdapter : IDeviceAdapter
{
    private readonly ILogger<MqttDeviceAdapter> _logger;
    private readonly IMqttClientService _mqttService;
    private readonly DeviceShadowService _shadowService; // Phase 3
    private readonly Dictionary<string, DeviceSubscription> _subscriptions = new();
    private bool _isInitialized;

    public string ProtocolName => "MQTT";
    public bool IsConnected => _isInitialized && _mqttService != null;

    public MqttDeviceAdapter(
        ILogger<MqttDeviceAdapter> logger,
        IMqttClientService mqttService,
        DeviceShadowService shadowService)
    {
        _logger = logger;
        _mqttService = mqttService;
        _shadowService = shadowService;
    }

    // ... (InitializeAsync, ConnectAsync, DisconnectAsync, GetStateAsync remain similar but could read from Shadow) ...

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing MQTT device adapter");
        await _mqttService.ConnectAsync();
        _isInitialized = true;
    }

    public Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("MQTT device {DeviceId} marked as connected", deviceId);
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(deviceId, out var sub))
        {
            sub.IsActive = false;
        }
        return Task.CompletedTask;
    }

    public async Task<DeviceState> GetStateAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        // Phase 3: Try to read from Shadow first
        try 
        {
            var (reported, _) = await _shadowService.GetShadowAsync(deviceId);
            if (reported.Count > 0)
            {
                return new DeviceState
                {
                    DeviceId = deviceId,
                    IsOnline = true,
                    LastUpdated = DateTime.UtcNow,
                    Properties = reported,
                    IsOn = reported.TryGetValue("state", out var s) && s?.ToString() == "ON"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read shadow for {DeviceId}", deviceId);
        }

        // Fallback to in-memory if shadow fails or empty
        var state = new DeviceState
        {
            DeviceId = deviceId,
            IsOnline = true, 
            LastUpdated = DateTime.UtcNow
        };

        if (_subscriptions.TryGetValue(deviceId, out var sub))
        {
            state.Properties = new Dictionary<string, object>(sub.CachedProperties);
            state.IsOn = sub.CachedProperties.TryGetValue("state", out var stateVal) && 
                        stateVal?.ToString()?.ToLowerInvariant() == "on";
        }

        return state;
    }

    public async Task<CommandResult> ExecuteCommandAsync(string deviceId, DeviceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing MQTT command {CommandName} on device {DeviceId}", command.CommandName, deviceId);

        try
        {
            var topic = BuildCommandTopic(deviceId, command.CommandName);
            var payload = System.Text.Json.JsonSerializer.Serialize(command.Parameters);
            
            await _mqttService.PublishAsync(topic, payload);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MQTT command {CommandName}", command.CommandName);
            return CommandResult.Fail(ex.Message);
        }
    }

    public async Task<bool> SetPropertyAsync(string deviceId, string property, object value, CancellationToken cancellationToken = default)
    {
        try
        {
            var topic = $"nexushome/{deviceId}/set/{property}";
            var payload = value?.ToString() ?? "";
            await _mqttService.PublishAsync(topic, payload);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting MQTT property {Property}", property);
            return false;
        }
    }

    public Task<object?> GetPropertyAsync(string deviceId, string property, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(deviceId, out var sub) &&
            sub.CachedProperties.TryGetValue(property, out var value))
        {
            return Task.FromResult<object?>(value);
        }
        return Task.FromResult<object?>(null);
    }

    public async Task SubscribeAsync(string deviceId, Action<DeviceStateChange> onStateChange, CancellationToken cancellationToken = default)
    {
        var statusTopic = $"nexushome/{deviceId}/status/#";
        
        _subscriptions[deviceId] = new DeviceSubscription
        {
            DeviceId = deviceId,
            Topic = statusTopic,
            Callback = onStateChange,
            IsActive = true
        };

        await _mqttService.SubscribeAsync(statusTopic, async (topic, payload) =>
        {
            // Parse topic to get property name
            var topicParts = topic.Split('/');
            var property = topicParts.Length > 3 ? topicParts[3] : "state";
            
            // Phase 3: Update Shadow State (Resilience)
            var props = new Dictionary<string, object> { { property, payload } };
            await _shadowService.UpdateReportedStateAsync(deviceId, props);

            if (_subscriptions.TryGetValue(deviceId, out var sub) && sub.IsActive)
            {
                var change = new DeviceStateChange
                {
                    DeviceId = deviceId,
                    PropertyName = property,
                    OldValue = sub.CachedProperties.TryGetValue(property, out var old) ? old : null,
                    NewValue = payload,
                    Timestamp = DateTime.UtcNow
                };

                sub.CachedProperties[property] = payload;
                sub.Callback?.Invoke(change);
            }
        });
    }

    public Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // MQTT discovery via special topic
        _logger.LogInformation("MQTT device discovery not implemented - devices self-announce");
        return Task.FromResult(Enumerable.Empty<DiscoveredDevice>());
    }

    public async ValueTask DisposeAsync()
    {
        await _mqttService.DisconnectAsync();
        _subscriptions.Clear();
        _isInitialized = false;
    }

    private static string BuildCommandTopic(string deviceId, string command)
    {
        return command.ToLowerInvariant() switch
        {
            "turnon" => $"nexushome/{deviceId}/command/on",
            "turnoff" => $"nexushome/{deviceId}/command/off",
            "toggle" => $"nexushome/{deviceId}/command/toggle",
            "setlevel" => $"nexushome/{deviceId}/command/level",
            "settemperature" => $"nexushome/{deviceId}/command/temperature",
            _ => $"nexushome/{deviceId}/command/{command.ToLowerInvariant()}"
        };
    }

    private class DeviceSubscription
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Action<DeviceStateChange>? Callback { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> CachedProperties { get; set; } = new();
    }
}
