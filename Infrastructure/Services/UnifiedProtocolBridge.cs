using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Data;

namespace NexusHome.IoT.Infrastructure.Services;

/// <summary>
/// Unified protocol bridge for routing device commands across multiple protocols.
/// Automatically selects the appropriate adapter based on device configuration.
/// </summary>
public class UnifiedProtocolBridge : IProtocolBridge, IAsyncDisposable
{
    private readonly ILogger<UnifiedProtocolBridge> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IDeviceAdapter> _adapters = new();
    private readonly ConcurrentDictionary<string, string> _deviceProtocolMap = new();
    private readonly List<Action<DeviceStateChange>> _globalSubscribers = new();

    public UnifiedProtocolBridge(
        ILogger<UnifiedProtocolBridge> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void RegisterAdapter(IDeviceAdapter adapter)
    {
        _adapters[adapter.ProtocolName] = adapter;
        _logger.LogInformation("Registered device adapter: {ProtocolName}", adapter.ProtocolName);
    }

    public IDeviceAdapter? GetAdapterForProtocol(string protocolName)
    {
        _adapters.TryGetValue(protocolName, out var adapter);
        return adapter;
    }

    public async Task<IDeviceAdapter?> GetAdapterForDeviceAsync(string deviceId)
    {
        // Check cached mapping
        if (_deviceProtocolMap.TryGetValue(deviceId, out var protocol))
        {
            return GetAdapterForProtocol(protocol);
        }

        // Lookup device in database to determine protocol
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var device = await context.SmartDevices
            .Where(d => d.UniqueDeviceIdentifier == deviceId || d.Id.ToString() == deviceId)
            .Select(d => new { d.ConnectionProtocol })
            .FirstOrDefaultAsync();

        if (device != null)
        {
            var protocolName = MapProtocolToAdapterName(device.ConnectionProtocol);
            _deviceProtocolMap[deviceId] = protocolName;
            return GetAdapterForProtocol(protocolName);
        }

        // Default to MQTT if unknown
        _logger.LogWarning("Unknown device {DeviceId}, defaulting to MQTT adapter", deviceId);
        return GetAdapterForProtocol("MQTT");
    }

    public async Task<CommandResult> ExecuteCommandAsync(string deviceId, DeviceCommand command, CancellationToken cancellationToken = default)
    {
        var adapter = await GetAdapterForDeviceAsync(deviceId);
        if (adapter == null)
        {
            return CommandResult.Fail($"No adapter found for device {deviceId}");
        }

        _logger.LogDebug("Routing command {CommandName} for device {DeviceId} via {Protocol}", 
            command.CommandName, deviceId, adapter.ProtocolName);

        return await adapter.ExecuteCommandAsync(deviceId, command, cancellationToken);
    }

    public async Task<DeviceState?> GetDeviceStateAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var adapter = await GetAdapterForDeviceAsync(deviceId);
        if (adapter == null)
        {
            return null;
        }

        return await adapter.GetStateAsync(deviceId, cancellationToken);
    }

    public Task BroadcastDeviceUpdateAsync(DeviceStateChange change)
    {
        _logger.LogDebug("Broadcasting state change for device {DeviceId}: {Property} = {Value}", 
            change.DeviceId, change.PropertyName, change.NewValue);

        foreach (var subscriber in _globalSubscribers)
        {
            try
            {
                subscriber(change);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying subscriber of device state change");
            }
        }

        return Task.CompletedTask;
    }

    public void SubscribeToAllDeviceUpdates(Action<DeviceStateChange> callback)
    {
        _globalSubscribers.Add(callback);
    }

    public IReadOnlyCollection<IDeviceAdapter> GetRegisteredAdapters()
    {
        return _adapters.Values.ToList().AsReadOnly();
    }

    public async Task<IEnumerable<DiscoveredDevice>> DiscoverAllDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var allDevices = new List<DiscoveredDevice>();
        var tasks = new List<Task<IEnumerable<DiscoveredDevice>>>();

        foreach (var adapter in _adapters.Values)
        {
            if (adapter.IsConnected)
            {
                tasks.Add(adapter.DiscoverDevicesAsync(timeout, cancellationToken));
            }
        }

        if (tasks.Any())
        {
            var results = await Task.WhenAll(tasks);
            allDevices.AddRange(results.SelectMany(r => r));
        }

        _logger.LogInformation("Discovered {Count} devices across all protocols", allDevices.Count);
        return allDevices;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var adapter in _adapters.Values)
        {
            await adapter.DisposeAsync();
        }
        _adapters.Clear();
        _deviceProtocolMap.Clear();
    }

    private static string MapProtocolToAdapterName(CommunicationProtocol protocol)
    {
        return protocol switch
        {
            CommunicationProtocol.MatterProtocol => "Matter",
            CommunicationProtocol.MqttMessaging => "MQTT",
            CommunicationProtocol.ZigbeeMesh => "Zigbee",
            CommunicationProtocol.ZWaveNetwork => "ZWave",
            CommunicationProtocol.WiFiConnection => "WiFi",
            CommunicationProtocol.BluetoothConnection => "Bluetooth",
            CommunicationProtocol.ThreadNetwork => "Thread",
            _ => "MQTT" // Default fallback
        };
    }
}
