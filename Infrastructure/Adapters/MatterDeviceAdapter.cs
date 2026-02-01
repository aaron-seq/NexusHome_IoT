using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Matter.Clusters;

namespace NexusHome.IoT.Infrastructure.Adapters;

/// <summary>
/// Device adapter for Matter protocol devices.
/// Routes commands to appropriate cluster handlers.
/// </summary>
public class MatterDeviceAdapter : IDeviceAdapter
{
    private readonly ILogger<MatterDeviceAdapter> _logger;
    private readonly IMatterService _matterService;
    private readonly Dictionary<string, MatterDeviceContext> _connectedDevices = new();
    private readonly Dictionary<uint, Func<ILogger, ClusterHandlerBase>> _clusterFactories;
    private bool _isInitialized;

    public string ProtocolName => "Matter";
    public bool IsConnected => _isInitialized;

    public MatterDeviceAdapter(
        ILogger<MatterDeviceAdapter> logger,
        IMatterService matterService)
    {
        _logger = logger;
        _matterService = matterService;
        
        // Register cluster handler factories
        _clusterFactories = new Dictionary<uint, Func<ILogger, ClusterHandlerBase>>
        {
            { 0x0006, log => new OnOffClusterHandler(new Logger<OnOffClusterHandler>(new LoggerFactory())) },
            { 0x0008, log => new LevelControlClusterHandler(new Logger<LevelControlClusterHandler>(new LoggerFactory())) },
            { 0x0201, log => new ThermostatClusterHandler(new Logger<ThermostatClusterHandler>(new LoggerFactory())) },
            { 0x0101, log => new DoorLockClusterHandler(new Logger<DoorLockClusterHandler>(new LoggerFactory())) },
        };
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Matter device adapter");
        await _matterService.StartDiscoveryAsync();
        _isInitialized = true;
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (_connectedDevices.ContainsKey(deviceId))
        {
            return true;
        }

        var success = await _matterService.ConnectToDeviceAsync(deviceId);
        if (success)
        {
            _connectedDevices[deviceId] = new MatterDeviceContext
            {
                DeviceId = deviceId,
                ConnectedAt = DateTime.UtcNow,
                ClusterHandlers = new Dictionary<uint, ClusterHandlerBase>()
            };
            _logger.LogInformation("Connected to Matter device {DeviceId}", deviceId);
        }
        return success;
    }

    public Task DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (_connectedDevices.Remove(deviceId))
        {
            _logger.LogInformation("Disconnected from Matter device {DeviceId}", deviceId);
        }
        return Task.CompletedTask;
    }

    public async Task<DeviceState> GetStateAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var state = new DeviceState
        {
            DeviceId = deviceId,
            IsOnline = _connectedDevices.ContainsKey(deviceId),
            LastUpdated = DateTime.UtcNow
        };

        if (_connectedDevices.TryGetValue(deviceId, out var context))
        {
            // Read common attributes
            try
            {
                var onOffResult = await _matterService.ReadAttributeAsync(deviceId, "0x0006", "0x0000");
                if (onOffResult is bool isOn)
                {
                    state.IsOn = isOn;
                    state.Properties["OnOff"] = isOn;
                }

                var levelResult = await _matterService.ReadAttributeAsync(deviceId, "0x0008", "0x0000");
                if (levelResult is byte level)
                {
                    state.Properties["Level"] = level;
                    state.Properties["LevelPercent"] = level * 100 / 254;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading state for device {DeviceId}", deviceId);
            }
        }

        return state;
    }

    public async Task<CommandResult> ExecuteCommandAsync(string deviceId, DeviceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing command {CommandName} on Matter device {DeviceId}", command.CommandName, deviceId);

        try
        {
            switch (command.CommandName.ToLowerInvariant())
            {
                case "turnon":
                    await _matterService.SendCommandAsync(deviceId, "OnOff.On", new { });
                    return CommandResult.Ok();

                case "turnoff":
                    await _matterService.SendCommandAsync(deviceId, "OnOff.Off", new { });
                    return CommandResult.Ok();

                case "toggle":
                    await _matterService.SendCommandAsync(deviceId, "OnOff.Toggle", new { });
                    return CommandResult.Ok();

                case "setlevel":
                    if (command.Parameters.TryGetValue("Level", out var levelObj))
                    {
                        var level = Convert.ToByte(levelObj);
                        await _matterService.SendCommandAsync(deviceId, "LevelControl.MoveToLevel", new { Level = level });
                        return CommandResult.Ok();
                    }
                    return CommandResult.Fail("Level parameter required");

                case "settemperature":
                    if (command.Parameters.TryGetValue("Temperature", out var tempObj))
                    {
                        var temp = Convert.ToDecimal(tempObj);
                        await _matterService.WriteAttributeAsync(deviceId, "0x0201", "0x0011", (short)(temp * 100));
                        return CommandResult.Ok();
                    }
                    return CommandResult.Fail("Temperature parameter required");

                case "lock":
                    await _matterService.SendCommandAsync(deviceId, "DoorLock.LockDoor", new { });
                    return CommandResult.Ok();

                case "unlock":
                    await _matterService.SendCommandAsync(deviceId, "DoorLock.UnlockDoor", new { });
                    return CommandResult.Ok();

                default:
                    // Generic command passthrough
                    await _matterService.SendCommandAsync(deviceId, command.CommandName, command.Parameters);
                    return CommandResult.Ok();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command {CommandName} on device {DeviceId}", command.CommandName, deviceId);
            return CommandResult.Fail(ex.Message);
        }
    }

    public async Task<bool> SetPropertyAsync(string deviceId, string property, object value, CancellationToken cancellationToken = default)
    {
        try
        {
            var (clusterId, attributeId) = MapPropertyToAttribute(property);
            await _matterService.WriteAttributeAsync(deviceId, clusterId, attributeId, value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting property {Property} on device {DeviceId}", property, deviceId);
            return false;
        }
    }

    public async Task<object?> GetPropertyAsync(string deviceId, string property, CancellationToken cancellationToken = default)
    {
        try
        {
            var (clusterId, attributeId) = MapPropertyToAttribute(property);
            return await _matterService.ReadAttributeAsync(deviceId, clusterId, attributeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property {Property} from device {DeviceId}", property, deviceId);
            return null;
        }
    }

    public Task SubscribeAsync(string deviceId, Action<DeviceStateChange> onStateChange, CancellationToken cancellationToken = default)
    {
        if (_connectedDevices.TryGetValue(deviceId, out var context))
        {
            context.StateChangeCallback = onStateChange;
        }
        return _matterService.SubscribeToEventsAsync(deviceId);
    }

    public async Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // Matter discovery via mDNS would go here
        // For now, return empty list - real implementation would scan network
        _logger.LogInformation("Starting Matter device discovery for {Timeout}s", timeout.TotalSeconds);
        await Task.Delay(timeout, cancellationToken);
        return Enumerable.Empty<DiscoveredDevice>();
    }

    public async ValueTask DisposeAsync()
    {
        await _matterService.StopDiscoveryAsync();
        _connectedDevices.Clear();
        _isInitialized = false;
    }

    private static (string ClusterId, string AttributeId) MapPropertyToAttribute(string property)
    {
        return property.ToLowerInvariant() switch
        {
            "onoff" or "ison" => ("0x0006", "0x0000"),
            "level" or "brightness" => ("0x0008", "0x0000"),
            "temperature" or "localtemperature" => ("0x0201", "0x0000"),
            "coolingsetpoint" => ("0x0201", "0x0011"),
            "heatingsetpoint" => ("0x0201", "0x0012"),
            "lockstate" => ("0x0101", "0x0000"),
            _ => throw new ArgumentException($"Unknown property: {property}")
        };
    }

    private class MatterDeviceContext
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public Dictionary<uint, ClusterHandlerBase> ClusterHandlers { get; set; } = new();
        public Action<DeviceStateChange>? StateChangeCallback { get; set; }
    }
}
