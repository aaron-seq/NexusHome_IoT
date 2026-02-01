using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Matter.Clusters;

/// <summary>
/// Matter Level Control Cluster Handler (Cluster ID: 0x0008).
/// Controls brightness/level for dimmable lights and similar devices.
/// </summary>
public class LevelControlClusterHandler : ClusterHandlerBase
{
    public override uint ClusterId => 0x0008;
    public override string ClusterName => "LevelControl";

    // Attribute IDs
    private const uint ATTR_CURRENT_LEVEL = 0x0000;
    private const uint ATTR_REMAINING_TIME = 0x0001;
    private const uint ATTR_MIN_LEVEL = 0x0002;
    private const uint ATTR_MAX_LEVEL = 0x0003;
    private const uint ATTR_ON_OFF_TRANSITION_TIME = 0x0010;
    private const uint ATTR_ON_LEVEL = 0x0011;
    private const uint ATTR_ON_TRANSITION_TIME = 0x0012;
    private const uint ATTR_OFF_TRANSITION_TIME = 0x0013;
    private const uint ATTR_DEFAULT_MOVE_RATE = 0x0014;
    private const uint ATTR_OPTIONS = 0x000F;
    private const uint ATTR_START_UP_CURRENT_LEVEL = 0x4000;

    // Command IDs
    private const uint CMD_MOVE_TO_LEVEL = 0x00;
    private const uint CMD_MOVE = 0x01;
    private const uint CMD_STEP = 0x02;
    private const uint CMD_STOP = 0x03;
    private const uint CMD_MOVE_TO_LEVEL_WITH_ON_OFF = 0x04;
    private const uint CMD_MOVE_WITH_ON_OFF = 0x05;
    private const uint CMD_STEP_WITH_ON_OFF = 0x06;
    private const uint CMD_STOP_WITH_ON_OFF = 0x07;

    // Current state
    private byte _currentLevel = 0;
    private ushort _remainingTime = 0;
    private byte _minLevel = 1;
    private byte _maxLevel = 254;
    private ushort _onOffTransitionTime = 0;
    private byte? _onLevel = null;
    private byte _startUpCurrentLevel = 0;

    // Events
    public event Action<byte>? OnLevelChanged;

    public LevelControlClusterHandler(ILogger<LevelControlClusterHandler> logger) : base(logger)
    {
    }

    public override async Task<ClusterCommandResult> HandleCommandAsync(uint commandId, byte[]? payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("LevelControl cluster handling command {CommandId}", commandId);

        switch (commandId)
        {
            case CMD_MOVE_TO_LEVEL:
            case CMD_MOVE_TO_LEVEL_WITH_ON_OFF:
                if (payload != null && payload.Length >= 3)
                {
                    var targetLevel = payload[0];
                    var transitionTime = BitConverter.ToUInt16(payload, 1);
                    await MoveToLevelAsync(targetLevel, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_STEP:
            case CMD_STEP_WITH_ON_OFF:
                if (payload != null && payload.Length >= 4)
                {
                    var stepMode = (StepMode)payload[0];
                    var stepSize = payload[1];
                    var transitionTime = BitConverter.ToUInt16(payload, 2);
                    await StepAsync(stepMode, stepSize, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_MOVE:
            case CMD_MOVE_WITH_ON_OFF:
                if (payload != null && payload.Length >= 2)
                {
                    var moveMode = (MoveMode)payload[0];
                    var rate = payload[1];
                    await MoveAsync(moveMode, rate, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_STOP:
            case CMD_STOP_WITH_ON_OFF:
                _remainingTime = 0;
                _logger.LogInformation("Level transition stopped at {Level}", _currentLevel);
                return ClusterCommandResult.Ok();

            default:
                _logger.LogWarning("Unsupported LevelControl command: {CommandId}", commandId);
                return ClusterCommandResult.Fail($"Unsupported command: 0x{commandId:X2}", 0x81);
        }
    }

    private Task MoveToLevelAsync(byte targetLevel, ushort transitionTime, CancellationToken cancellationToken)
    {
        targetLevel = Math.Clamp(targetLevel, _minLevel, _maxLevel);
        
        // For simplicity, instant transition (real implementation would animate)
        _currentLevel = targetLevel;
        _remainingTime = 0;
        OnLevelChanged?.Invoke(_currentLevel);
        
        _logger.LogInformation("Level set to {Level} (transition: {TransitionTime}ms)", 
            _currentLevel, transitionTime * 100);
        
        return Task.CompletedTask;
    }

    private Task StepAsync(StepMode mode, byte stepSize, ushort transitionTime, CancellationToken cancellationToken)
    {
        int newLevel = mode == StepMode.Up 
            ? _currentLevel + stepSize 
            : _currentLevel - stepSize;
        
        _currentLevel = (byte)Math.Clamp(newLevel, _minLevel, _maxLevel);
        OnLevelChanged?.Invoke(_currentLevel);
        
        _logger.LogInformation("Level stepped {Mode} by {StepSize} to {Level}", 
            mode, stepSize, _currentLevel);
        
        return Task.CompletedTask;
    }

    private Task MoveAsync(MoveMode mode, byte rate, CancellationToken cancellationToken)
    {
        // Simplified: instant move to min/max
        _currentLevel = mode == MoveMode.Up ? _maxLevel : _minLevel;
        OnLevelChanged?.Invoke(_currentLevel);
        
        _logger.LogInformation("Level moved {Mode} to {Level}", mode, _currentLevel);
        
        return Task.CompletedTask;
    }

    public override Task<ClusterAttributeValue> ReadAttributeAsync(uint attributeId, CancellationToken cancellationToken = default)
    {
        ClusterAttributeValue result = attributeId switch
        {
            ATTR_CURRENT_LEVEL => new() { AttributeId = attributeId, Value = _currentLevel, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_REMAINING_TIME => new() { AttributeId = attributeId, Value = _remainingTime, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_MIN_LEVEL => new() { AttributeId = attributeId, Value = _minLevel, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_MAX_LEVEL => new() { AttributeId = attributeId, Value = _maxLevel, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_ON_OFF_TRANSITION_TIME => new() { AttributeId = attributeId, Value = _onOffTransitionTime, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_ON_LEVEL => new() { AttributeId = attributeId, Value = _onLevel, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_START_UP_CURRENT_LEVEL => new() { AttributeId = attributeId, Value = _startUpCurrentLevel, Type = ClusterAttributeType.UInt8, Success = true },
            _ => new() { AttributeId = attributeId, Success = false, ErrorMessage = $"Unknown attribute: 0x{attributeId:X4}" }
        };
        
        return Task.FromResult(result);
    }

    public override Task<bool> WriteAttributeAsync(uint attributeId, object value, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (attributeId)
            {
                case ATTR_ON_OFF_TRANSITION_TIME:
                    _onOffTransitionTime = Convert.ToUInt16(value);
                    return Task.FromResult(true);
                    
                case ATTR_ON_LEVEL:
                    _onLevel = value == null ? null : Convert.ToByte(value);
                    return Task.FromResult(true);
                    
                case ATTR_START_UP_CURRENT_LEVEL:
                    _startUpCurrentLevel = Convert.ToByte(value);
                    return Task.FromResult(true);
                    
                default:
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
        new() { AttributeId = ATTR_CURRENT_LEVEL, Name = "CurrentLevel", Type = ClusterAttributeType.UInt8, IsReportable = true },
        new() { AttributeId = ATTR_REMAINING_TIME, Name = "RemainingTime", Type = ClusterAttributeType.UInt16 },
        new() { AttributeId = ATTR_MIN_LEVEL, Name = "MinLevel", Type = ClusterAttributeType.UInt8 },
        new() { AttributeId = ATTR_MAX_LEVEL, Name = "MaxLevel", Type = ClusterAttributeType.UInt8 },
        new() { AttributeId = ATTR_ON_OFF_TRANSITION_TIME, Name = "OnOffTransitionTime", Type = ClusterAttributeType.UInt16, IsWritable = true },
        new() { AttributeId = ATTR_ON_LEVEL, Name = "OnLevel", Type = ClusterAttributeType.UInt8, IsWritable = true },
        new() { AttributeId = ATTR_START_UP_CURRENT_LEVEL, Name = "StartUpCurrentLevel", Type = ClusterAttributeType.UInt8, IsWritable = true },
    };

    public override IReadOnlyList<ClusterCommand> GetSupportedCommands() => new List<ClusterCommand>
    {
        new() { CommandId = CMD_MOVE_TO_LEVEL, Name = "MoveToLevel" },
        new() { CommandId = CMD_MOVE, Name = "Move" },
        new() { CommandId = CMD_STEP, Name = "Step" },
        new() { CommandId = CMD_STOP, Name = "Stop" },
        new() { CommandId = CMD_MOVE_TO_LEVEL_WITH_ON_OFF, Name = "MoveToLevelWithOnOff" },
        new() { CommandId = CMD_STEP_WITH_ON_OFF, Name = "StepWithOnOff" },
    };

    // Public API
    public byte CurrentLevel => _currentLevel;
    public byte MinLevel => _minLevel;
    public byte MaxLevel => _maxLevel;
    
    public void SetLevel(byte level) => MoveToLevelAsync(level, 0, default);
    public void SetLevelPercent(int percent) => SetLevel((byte)(percent * 254 / 100));
    public int GetLevelPercent() => _currentLevel * 100 / 254;
}

public enum StepMode : byte
{
    Up = 0,
    Down = 1
}

public enum MoveMode : byte
{
    Up = 0,
    Down = 1
}
