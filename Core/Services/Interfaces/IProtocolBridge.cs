namespace NexusHome.IoT.Core.Services.Interfaces;

/// <summary>
/// Protocol bridge for routing commands across different device protocols.
/// </summary>
public interface IProtocolBridge
{
    /// <summary>
    /// Gets the appropriate adapter for a specific device.
    /// </summary>
    Task<IDeviceAdapter?> GetAdapterForDeviceAsync(string deviceId);

    /// <summary>
    /// Gets an adapter for a specific protocol type.
    /// </summary>
    IDeviceAdapter? GetAdapterForProtocol(string protocolName);

    /// <summary>
    /// Registers a device adapter with the bridge.
    /// </summary>
    void RegisterAdapter(IDeviceAdapter adapter);

    /// <summary>
    /// Broadcasts a device state change to all interested subscribers.
    /// </summary>
    Task BroadcastDeviceUpdateAsync(DeviceStateChange change);

    /// <summary>
    /// Executes a command on a device, automatically selecting the correct protocol.
    /// </summary>
    Task<CommandResult> ExecuteCommandAsync(string deviceId, DeviceCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of a device, automatically selecting the correct protocol.
    /// </summary>
    Task<DeviceState?> GetDeviceStateAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered protocol adapters.
    /// </summary>
    IReadOnlyCollection<IDeviceAdapter> GetRegisteredAdapters();

    /// <summary>
    /// Discovers all devices across all protocols.
    /// </summary>
    Task<IEnumerable<DiscoveredDevice>> DiscoverAllDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
