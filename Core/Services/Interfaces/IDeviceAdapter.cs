namespace NexusHome.IoT.Core.Services.Interfaces;

/// <summary>
/// Protocol-agnostic device adapter interface for multi-protocol smart home communication.
/// </summary>
public interface IDeviceAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the protocol name (e.g., "Matter", "MQTT", "Zigbee").
    /// </summary>
    string ProtocolName { get; }
    
    /// <summary>
    /// Gets whether the adapter is currently connected and operational.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Initializes the adapter and establishes connection to the protocol network.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a specific device.
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <returns>True if connection successful</returns>
    Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from a specific device.
    /// </summary>
    Task DisconnectAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a device.
    /// </summary>
    Task<DeviceState> GetStateAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command on a device.
    /// </summary>
    Task<CommandResult> ExecuteCommandAsync(string deviceId, DeviceCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a property value on a device.
    /// </summary>
    Task<bool> SetPropertyAsync(string deviceId, string property, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a property value from a device.
    /// </summary>
    Task<object?> GetPropertyAsync(string deviceId, string property, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to device state changes.
    /// </summary>
    Task SubscribeAsync(string deviceId, Action<DeviceStateChange> onStateChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers devices available on this protocol.
    /// </summary>
    Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the current state of a device.
/// </summary>
public class DeviceState
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsOn { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Represents a command to execute on a device.
/// </summary>
public class DeviceCommand
{
    public string CommandName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public static DeviceCommand TurnOn() => new() { CommandName = "TurnOn" };
    public static DeviceCommand TurnOff() => new() { CommandName = "TurnOff" };
    public static DeviceCommand Toggle() => new() { CommandName = "Toggle" };
    public static DeviceCommand SetLevel(int level) => new() { CommandName = "SetLevel", Parameters = { ["Level"] = level } };
    public static DeviceCommand SetTemperature(decimal temp) => new() { CommandName = "SetTemperature", Parameters = { ["Temperature"] = temp } };
}

/// <summary>
/// Result of executing a device command.
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }
    
    public static CommandResult Ok(object? data = null) => new() { Success = true, Data = data };
    public static CommandResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Represents a change in device state.
/// </summary>
public class DeviceStateChange
{
    public string DeviceId { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a device discovered during network scan.
/// </summary>
public class DiscoveredDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool IsCommissioned { get; set; }
}
