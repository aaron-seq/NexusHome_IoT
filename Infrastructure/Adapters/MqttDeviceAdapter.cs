using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Infrastructure.Adapters;

/// <summary>
/// Device adapter for MQTT-based devices.
/// Provides backward compatibility with existing MQTT infrastructure.
/// </summary>
public class MqttDeviceAdapter : IDeviceAdapter
{
    private readonly ILogger<MqttDeviceAdapter> _logger;
    private readonly IMqttClientService _mqttService;
    private readonly Dictionary<string, DeviceSubscription> _subscriptions = new();
    private bool _isInitialized;

    public string ProtocolName => "MQTT";
    public bool IsConnected => _isInitialized && _mqttService != null;

    public MqttDeviceAdapter(
        ILogger<MqttDeviceAdapter> logger,
        IMqttClientService mqttService)
    {
        _logger = logger;
        _mqttService = mqttService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing MQTT device adapter");
        await _mqttService.ConnectAsync();
        _isInitialized = true;
    }

    public Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        // MQTT devices don't require explicit connection - they connect to broker
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

    public Task<DeviceState> GetStateAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        // MQTT state is typically push-based; return cached state
        var state = new DeviceState
        {
            DeviceId = deviceId,
            IsOnline = true, // Assume online if we can query
            LastUpdated = DateTime.UtcNow
        };

        if (_subscriptions.TryGetValue(deviceId, out var sub))
        {
            state.Properties = new Dictionary<string, object>(sub.CachedProperties);
            state.IsOn = sub.CachedProperties.TryGetValue("state", out var stateVal) && 
                        stateVal?.ToString()?.ToLowerInvariant() == "on";
        }

        return Task.FromResult(state);
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
            if (_subscriptions.TryGetValue(deviceId, out var sub) && sub.IsActive)
            {
                // Parse topic to get property name
                var topicParts = topic.Split('/');
                var property = topicParts.Length > 3 ? topicParts[3] : "state";
                
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
            await Task.CompletedTask;
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
