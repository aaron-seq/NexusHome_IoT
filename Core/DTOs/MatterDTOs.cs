using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Core.DTOs;

public class MatterCommissioningResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ulong NodeId { get; set; }
    public MatterDevice? DeviceInfo { get; set; }
    public DateTime? CommissioningTime { get; set; }
}

public class MatterDevice
{
    public ulong NodeId { get; set; }
    public uint VendorId { get; set; }
    public uint ProductId { get; set; }
    public uint DeviceType { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public ulong FabricId { get; set; }
    public List<uint> SupportedClusters { get; set; } = new();
    public MatterNetworkType NetworkType { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CommissionedAt { get; set; }
    public DateTime LastSeen { get; set; }
}

public class MatterDeviceInfo
{
    public ulong NodeId { get; set; }
    public uint VendorId { get; set; }
    public uint ProductId { get; set; }
    public uint DeviceType { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string HardwareVersion { get; set; } = string.Empty;
    public string SoftwareVersion { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public List<uint> SupportedClusters { get; set; } = new();
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}

public class MatterAttributeValue
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? StringValue => Value?.ToString();
    public MatterAttributeType Type { get; set; }
}

public class MatterDeviceEventArgs : EventArgs
{
    public MatterDevice Device { get; set; } = null!;
}

public class MatterAttributeEventArgs : EventArgs
{
    public ulong NodeId { get; set; }
    public uint ClusterId { get; set; }
    public uint AttributeId { get; set; }
    public object? Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MatterFabric
{
    public ulong FabricId { get; set; }
    public string FabricLabel { get; set; } = string.Empty;
    public string RootPublicKey { get; set; } = string.Empty;
    public uint VendorId { get; set; }
    public ulong NodeId { get; set; }
    public bool IsActive { get; set; }
    public int CommissionedDeviceCount { get; set; }
}

public class MatterNetworkInfo
{
    public ulong FabricId { get; set; }
    public ulong NodeId { get; set; }
    public bool OperationalDatasetPresent { get; set; }
    public bool WiFiConnected { get; set; }
    public bool ThreadEnabled { get; set; }
    public bool IPv6Enabled { get; set; }
    public int CommissionedDeviceCount { get; set; }
    public int ActiveSubscriptions { get; set; }
    public DateTime LastNetworkActivity { get; set; }
}

public enum MatterNetworkType
{
    WiFi,
    Thread,
    Ethernet
}

public enum MatterAttributeType
{
    Boolean,
    Integer,
    UnsignedInteger,
    Float,
    String,
    OctetString,
    Array,
    Structure,
    Null
}

// Internal class for session
public class MatterCommissioningSession
{
    public string SessionId { get; set; } = string.Empty;
    public string SetupCode { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}
