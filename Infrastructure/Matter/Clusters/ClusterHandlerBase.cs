using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Matter.Clusters;

/// <summary>
/// Base class for Matter cluster handlers.
/// </summary>
public abstract class ClusterHandlerBase
{
    protected readonly ILogger _logger;
    
    /// <summary>
    /// Matter cluster ID (e.g., 0x0006 for OnOff, 0x0008 for LevelControl).
    /// </summary>
    public abstract uint ClusterId { get; }
    
    /// <summary>
    /// Human-readable cluster name.
    /// </summary>
    public abstract string ClusterName { get; }

    protected ClusterHandlerBase(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles a command for this cluster.
    /// </summary>
    /// <param name="commandId">Matter command ID</param>
    /// <param name="payload">Command payload</param>
    /// <returns>Response payload if any</returns>
    public abstract Task<ClusterCommandResult> HandleCommandAsync(uint commandId, byte[]? payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an attribute value.
    /// </summary>
    public abstract Task<ClusterAttributeValue> ReadAttributeAsync(uint attributeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes an attribute value.
    /// </summary>
    public abstract Task<bool> WriteAttributeAsync(uint attributeId, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all supported attributes for this cluster.
    /// </summary>
    public abstract IReadOnlyList<ClusterAttribute> GetSupportedAttributes();

    /// <summary>
    /// Gets all supported commands for this cluster.
    /// </summary>
    public abstract IReadOnlyList<ClusterCommand> GetSupportedCommands();
}

public class ClusterCommandResult
{
    public bool Success { get; set; }
    public uint? StatusCode { get; set; }
    public byte[]? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static ClusterCommandResult Ok(byte[]? response = null) => new() { Success = true, ResponsePayload = response };
    public static ClusterCommandResult Fail(string error, uint statusCode = 0x01) => new() { Success = false, ErrorMessage = error, StatusCode = statusCode };
}

public class ClusterAttributeValue
{
    public uint AttributeId { get; set; }
    public object? Value { get; set; }
    public ClusterAttributeType Type { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ClusterAttribute
{
    public uint AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClusterAttributeType Type { get; set; }
    public bool IsReadable { get; set; } = true;
    public bool IsWritable { get; set; }
    public bool IsReportable { get; set; }
}

public class ClusterCommand
{
    public uint CommandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool RequiresResponse { get; set; }
}

public enum ClusterAttributeType
{
    Boolean,
    UInt8,
    UInt16,
    UInt32,
    Int8,
    Int16,
    Int32,
    Single,
    String,
    OctetString,
    Enum8,
    Enum16,
    Bitmap8,
    Bitmap16,
    Array,
    Struct
}
