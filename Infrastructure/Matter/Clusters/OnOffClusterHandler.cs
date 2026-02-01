using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Matter.Clusters;

/// <summary>
/// Matter OnOff Cluster Handler (Cluster ID: 0x0006).
/// Controls simple on/off devices like lights, switches, outlets.
/// </summary>
public class OnOffClusterHandler : ClusterHandlerBase
{
    public override uint ClusterId => 0x0006;
    public override string ClusterName => "OnOff";

    // Attribute IDs
    private const uint ATTR_ON_OFF = 0x0000;
    private const uint ATTR_GLOBAL_SCENE_CONTROL = 0x4000;
    private const uint ATTR_ON_TIME = 0x4001;
    private const uint ATTR_OFF_WAIT_TIME = 0x4002;
    private const uint ATTR_START_UP_ON_OFF = 0x4003;

    // Command IDs
    private const uint CMD_OFF = 0x00;
    private const uint CMD_ON = 0x01;
    private const uint CMD_TOGGLE = 0x02;
    private const uint CMD_OFF_WITH_EFFECT = 0x40;
    private const uint CMD_ON_WITH_RECALL_GLOBAL_SCENE = 0x41;
    private const uint CMD_ON_WITH_TIMED_OFF = 0x42;

    // Current state
    private bool _isOn;
    private ushort _onTime;
    private ushort _offWaitTime;
    private StartUpOnOffEnum _startUpOnOff = StartUpOnOffEnum.Off;

    // Event for state changes
    public event Action<bool>? OnStateChanged;

    public OnOffClusterHandler(ILogger<OnOffClusterHandler> logger) : base(logger)
    {
    }

    public override async Task<ClusterCommandResult> HandleCommandAsync(uint commandId, byte[]? payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OnOff cluster handling command {CommandId}", commandId);

        switch (commandId)
        {
            case CMD_OFF:
                _isOn = false;
                OnStateChanged?.Invoke(_isOn);
                _logger.LogInformation("Device turned OFF");
                return ClusterCommandResult.Ok();

            case CMD_ON:
                _isOn = true;
                OnStateChanged?.Invoke(_isOn);
                _logger.LogInformation("Device turned ON");
                return ClusterCommandResult.Ok();

            case CMD_TOGGLE:
                _isOn = !_isOn;
                OnStateChanged?.Invoke(_isOn);
                _logger.LogInformation("Device toggled to {State}", _isOn ? "ON" : "OFF");
                return ClusterCommandResult.Ok();

            case CMD_ON_WITH_TIMED_OFF:
                if (payload != null && payload.Length >= 6)
                {
                    var onOffControl = payload[0];
                    _onTime = BitConverter.ToUInt16(payload, 1);
                    _offWaitTime = BitConverter.ToUInt16(payload, 3);
                    _isOn = true;
                    OnStateChanged?.Invoke(_isOn);
                    _logger.LogInformation("Device ON with timed off: {OnTime}ds, wait: {OffWaitTime}ds", 
                        _onTime / 10.0, _offWaitTime / 10.0);
                }
                return ClusterCommandResult.Ok();

            default:
                _logger.LogWarning("Unsupported OnOff command: {CommandId}", commandId);
                return ClusterCommandResult.Fail($"Unsupported command: 0x{commandId:X2}", 0x81);
        }
    }

    public override Task<ClusterAttributeValue> ReadAttributeAsync(uint attributeId, CancellationToken cancellationToken = default)
    {
        ClusterAttributeValue result = attributeId switch
        {
            ATTR_ON_OFF => new ClusterAttributeValue { AttributeId = attributeId, Value = _isOn, Type = ClusterAttributeType.Boolean, Success = true },
            ATTR_ON_TIME => new ClusterAttributeValue { AttributeId = attributeId, Value = _onTime, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_OFF_WAIT_TIME => new ClusterAttributeValue { AttributeId = attributeId, Value = _offWaitTime, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_START_UP_ON_OFF => new ClusterAttributeValue { AttributeId = attributeId, Value = (byte)_startUpOnOff, Type = ClusterAttributeType.Enum8, Success = true },
            _ => new ClusterAttributeValue { AttributeId = attributeId, Success = false, ErrorMessage = $"Unknown attribute: 0x{attributeId:X4}" }
        };
        
        return Task.FromResult(result);
    }

    public override Task<bool> WriteAttributeAsync(uint attributeId, object value, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (attributeId)
            {
                case ATTR_ON_TIME:
                    _onTime = Convert.ToUInt16(value);
                    return Task.FromResult(true);
                    
                case ATTR_OFF_WAIT_TIME:
                    _offWaitTime = Convert.ToUInt16(value);
                    return Task.FromResult(true);
                    
                case ATTR_START_UP_ON_OFF:
                    _startUpOnOff = (StartUpOnOffEnum)Convert.ToByte(value);
                    return Task.FromResult(true);
                    
                default:
                    _logger.LogWarning("Attempt to write read-only or unknown attribute: 0x{AttributeId:X4}", attributeId);
                    return Task.FromResult(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing attribute 0x{AttributeId:X4}", attributeId);
            return Task.FromResult(false);
        }
    }

    public override IReadOnlyList<ClusterAttribute> GetSupportedAttributes() => new List<ClusterAttribute>
    {
        new() { AttributeId = ATTR_ON_OFF, Name = "OnOff", Type = ClusterAttributeType.Boolean, IsReportable = true },
        new() { AttributeId = ATTR_ON_TIME, Name = "OnTime", Type = ClusterAttributeType.UInt16, IsWritable = true },
        new() { AttributeId = ATTR_OFF_WAIT_TIME, Name = "OffWaitTime", Type = ClusterAttributeType.UInt16, IsWritable = true },
        new() { AttributeId = ATTR_START_UP_ON_OFF, Name = "StartUpOnOff", Type = ClusterAttributeType.Enum8, IsWritable = true },
    };

    public override IReadOnlyList<ClusterCommand> GetSupportedCommands() => new List<ClusterCommand>
    {
        new() { CommandId = CMD_OFF, Name = "Off" },
        new() { CommandId = CMD_ON, Name = "On" },
        new() { CommandId = CMD_TOGGLE, Name = "Toggle" },
        new() { CommandId = CMD_ON_WITH_TIMED_OFF, Name = "OnWithTimedOff" },
    };

    // Public API for direct access
    public bool IsOn => _isOn;
    public void TurnOn() => HandleCommandAsync(CMD_ON, null);
    public void TurnOff() => HandleCommandAsync(CMD_OFF, null);
    public void Toggle() => HandleCommandAsync(CMD_TOGGLE, null);
}

public enum StartUpOnOffEnum : byte
{
    Off = 0,
    On = 1,
    Toggle = 2,
    Previous = 0xFF
}
