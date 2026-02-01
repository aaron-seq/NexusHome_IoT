using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Matter.Clusters;

/// <summary>
/// Matter Door Lock Cluster Handler (Cluster ID: 0x0101).
/// Controls smart locks including lock/unlock and PIN management.
/// </summary>
public class DoorLockClusterHandler : ClusterHandlerBase
{
    public override uint ClusterId => 0x0101;
    public override string ClusterName => "DoorLock";

    // Attribute IDs
    private const uint ATTR_LOCK_STATE = 0x0000;
    private const uint ATTR_LOCK_TYPE = 0x0001;
    private const uint ATTR_ACTUATOR_ENABLED = 0x0002;
    private const uint ATTR_DOOR_STATE = 0x0003;
    private const uint ATTR_NUM_LOCK_RECORDS_SUPPORTED = 0x0010;
    private const uint ATTR_NUM_TOTAL_USERS_SUPPORTED = 0x0011;
    private const uint ATTR_NUM_PIN_USERS_SUPPORTED = 0x0012;
    private const uint ATTR_MAX_PIN_CODE_LENGTH = 0x0017;
    private const uint ATTR_MIN_PIN_CODE_LENGTH = 0x0018;
    private const uint ATTR_AUTO_RELOCK_TIME = 0x0023;
    private const uint ATTR_OPERATING_MODE = 0x0025;
    private const uint ATTR_REQUIRE_PIN_FOR_REMOTE = 0x0033;

    // Command IDs
    private const uint CMD_LOCK_DOOR = 0x00;
    private const uint CMD_UNLOCK_DOOR = 0x01;
    private const uint CMD_UNLOCK_WITH_TIMEOUT = 0x03;
    private const uint CMD_SET_PIN_CODE = 0x05;
    private const uint CMD_GET_PIN_CODE = 0x06;
    private const uint CMD_CLEAR_PIN_CODE = 0x07;
    private const uint CMD_CLEAR_ALL_PIN_CODES = 0x08;

    // Current state
    private DoorLockState _lockState = DoorLockState.Unlocked;
    private DoorLockType _lockType = DoorLockType.DeadBolt;
    private bool _actuatorEnabled = true;
    private DoorState _doorState = DoorState.Closed;
    private uint _autoRelockTime = 0; // 0 = disabled
    private DoorLockOperatingMode _operatingMode = DoorLockOperatingMode.Normal;
    private bool _requirePinForRemote = true;
    private readonly Dictionary<ushort, string> _pinCodes = new();

    // Events
    public event Action<DoorLockState>? OnLockStateChanged;
    public event Action<DoorState>? OnDoorStateChanged;
    public event Action<ushort, DoorLockOperationEventCode>? OnLockOperationEvent;

    public DoorLockClusterHandler(ILogger<DoorLockClusterHandler> logger) : base(logger)
    {
    }

    public override async Task<ClusterCommandResult> HandleCommandAsync(uint commandId, byte[]? payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DoorLock cluster handling command {CommandId}", commandId);

        switch (commandId)
        {
            case CMD_LOCK_DOOR:
                return await LockDoorAsync(payload, cancellationToken);

            case CMD_UNLOCK_DOOR:
                return await UnlockDoorAsync(payload, cancellationToken);

            case CMD_UNLOCK_WITH_TIMEOUT:
                if (payload != null && payload.Length >= 2)
                {
                    var timeout = BitConverter.ToUInt16(payload, 0);
                    return await UnlockWithTimeoutAsync(timeout, cancellationToken);
                }
                return ClusterCommandResult.Fail("Invalid payload");

            case CMD_SET_PIN_CODE:
                return HandleSetPinCode(payload);

            case CMD_CLEAR_PIN_CODE:
                return HandleClearPinCode(payload);

            case CMD_CLEAR_ALL_PIN_CODES:
                _pinCodes.Clear();
                _logger.LogInformation("All PIN codes cleared");
                return ClusterCommandResult.Ok();

            default:
                _logger.LogWarning("Unsupported DoorLock command: {CommandId}", commandId);
                return ClusterCommandResult.Fail($"Unsupported command: 0x{commandId:X2}", 0x81);
        }
    }

    private Task<ClusterCommandResult> LockDoorAsync(byte[]? payload, CancellationToken cancellationToken)
    {
        if (!_actuatorEnabled)
        {
            return Task.FromResult(ClusterCommandResult.Fail("Actuator disabled"));
        }

        // Optional PIN verification
        if (_requirePinForRemote && payload != null && payload.Length > 0)
        {
            var pin = System.Text.Encoding.UTF8.GetString(payload);
            if (!_pinCodes.ContainsValue(pin))
            {
                OnLockOperationEvent?.Invoke(0, DoorLockOperationEventCode.InvalidCredential);
                return Task.FromResult(ClusterCommandResult.Fail("Invalid PIN", 0x02));
            }
        }

        _lockState = DoorLockState.Locked;
        OnLockStateChanged?.Invoke(_lockState);
        OnLockOperationEvent?.Invoke(0, DoorLockOperationEventCode.Lock);
        _logger.LogInformation("Door locked");

        return Task.FromResult(ClusterCommandResult.Ok());
    }

    private Task<ClusterCommandResult> UnlockDoorAsync(byte[]? payload, CancellationToken cancellationToken)
    {
        if (!_actuatorEnabled)
        {
            return Task.FromResult(ClusterCommandResult.Fail("Actuator disabled"));
        }

        _lockState = DoorLockState.Unlocked;
        OnLockStateChanged?.Invoke(_lockState);
        OnLockOperationEvent?.Invoke(0, DoorLockOperationEventCode.Unlock);
        _logger.LogInformation("Door unlocked");

        // Schedule auto-relock if configured
        if (_autoRelockTime > 0)
        {
            _ = Task.Delay(TimeSpan.FromSeconds(_autoRelockTime), cancellationToken)
                .ContinueWith(_ => LockDoorAsync(null, default), cancellationToken);
        }

        return Task.FromResult(ClusterCommandResult.Ok());
    }

    private async Task<ClusterCommandResult> UnlockWithTimeoutAsync(ushort timeoutSeconds, CancellationToken cancellationToken)
    {
        var result = await UnlockDoorAsync(null, cancellationToken);
        if (result.Success && timeoutSeconds > 0)
        {
            _ = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)
                .ContinueWith(_ => LockDoorAsync(null, default), cancellationToken);
            _logger.LogInformation("Door will auto-lock in {Timeout} seconds", timeoutSeconds);
        }
        return result;
    }

    private ClusterCommandResult HandleSetPinCode(byte[]? payload)
    {
        if (payload == null || payload.Length < 4)
        {
            return ClusterCommandResult.Fail("Invalid payload");
        }

        var userId = BitConverter.ToUInt16(payload, 0);
        var pinLength = payload[2];
        var pin = System.Text.Encoding.UTF8.GetString(payload, 3, pinLength);

        if (pin.Length < 4 || pin.Length > 8)
        {
            return ClusterCommandResult.Fail("Invalid PIN length (4-8 digits required)");
        }

        _pinCodes[userId] = pin;
        _logger.LogInformation("PIN code set for user {UserId}", userId);
        return ClusterCommandResult.Ok();
    }

    private ClusterCommandResult HandleClearPinCode(byte[]? payload)
    {
        if (payload == null || payload.Length < 2)
        {
            return ClusterCommandResult.Fail("Invalid payload");
        }

        var userId = BitConverter.ToUInt16(payload, 0);
        if (_pinCodes.Remove(userId))
        {
            _logger.LogInformation("PIN code cleared for user {UserId}", userId);
            return ClusterCommandResult.Ok();
        }
        return ClusterCommandResult.Fail("User not found");
    }

    public override Task<ClusterAttributeValue> ReadAttributeAsync(uint attributeId, CancellationToken cancellationToken = default)
    {
        ClusterAttributeValue result = attributeId switch
        {
            ATTR_LOCK_STATE => new() { AttributeId = attributeId, Value = (byte)_lockState, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_LOCK_TYPE => new() { AttributeId = attributeId, Value = (byte)_lockType, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_ACTUATOR_ENABLED => new() { AttributeId = attributeId, Value = _actuatorEnabled, Type = ClusterAttributeType.Boolean, Success = true },
            ATTR_DOOR_STATE => new() { AttributeId = attributeId, Value = (byte)_doorState, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_AUTO_RELOCK_TIME => new() { AttributeId = attributeId, Value = _autoRelockTime, Type = ClusterAttributeType.UInt32, Success = true },
            ATTR_OPERATING_MODE => new() { AttributeId = attributeId, Value = (byte)_operatingMode, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_REQUIRE_PIN_FOR_REMOTE => new() { AttributeId = attributeId, Value = _requirePinForRemote, Type = ClusterAttributeType.Boolean, Success = true },
            ATTR_MAX_PIN_CODE_LENGTH => new() { AttributeId = attributeId, Value = (byte)8, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_MIN_PIN_CODE_LENGTH => new() { AttributeId = attributeId, Value = (byte)4, Type = ClusterAttributeType.UInt8, Success = true },
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
                case ATTR_AUTO_RELOCK_TIME:
                    _autoRelockTime = Convert.ToUInt32(value);
                    return Task.FromResult(true);
                    
                case ATTR_OPERATING_MODE:
                    _operatingMode = (DoorLockOperatingMode)Convert.ToByte(value);
                    return Task.FromResult(true);
                    
                case ATTR_REQUIRE_PIN_FOR_REMOTE:
                    _requirePinForRemote = Convert.ToBoolean(value);
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
        new() { AttributeId = ATTR_LOCK_STATE, Name = "LockState", Type = ClusterAttributeType.Enum8, IsReportable = true },
        new() { AttributeId = ATTR_LOCK_TYPE, Name = "LockType", Type = ClusterAttributeType.Enum8 },
        new() { AttributeId = ATTR_ACTUATOR_ENABLED, Name = "ActuatorEnabled", Type = ClusterAttributeType.Boolean },
        new() { AttributeId = ATTR_DOOR_STATE, Name = "DoorState", Type = ClusterAttributeType.Enum8, IsReportable = true },
        new() { AttributeId = ATTR_AUTO_RELOCK_TIME, Name = "AutoRelockTime", Type = ClusterAttributeType.UInt32, IsWritable = true },
        new() { AttributeId = ATTR_OPERATING_MODE, Name = "OperatingMode", Type = ClusterAttributeType.Enum8, IsWritable = true },
        new() { AttributeId = ATTR_REQUIRE_PIN_FOR_REMOTE, Name = "RequirePINForRemoteOperation", Type = ClusterAttributeType.Boolean, IsWritable = true },
    };

    public override IReadOnlyList<ClusterCommand> GetSupportedCommands() => new List<ClusterCommand>
    {
        new() { CommandId = CMD_LOCK_DOOR, Name = "LockDoor" },
        new() { CommandId = CMD_UNLOCK_DOOR, Name = "UnlockDoor" },
        new() { CommandId = CMD_UNLOCK_WITH_TIMEOUT, Name = "UnlockWithTimeout" },
        new() { CommandId = CMD_SET_PIN_CODE, Name = "SetPINCode" },
        new() { CommandId = CMD_CLEAR_PIN_CODE, Name = "ClearPINCode" },
        new() { CommandId = CMD_CLEAR_ALL_PIN_CODES, Name = "ClearAllPINCodes" },
    };

    // Public API
    public DoorLockState LockState => _lockState;
    public DoorState DoorState => _doorState;
    public bool IsLocked => _lockState == DoorLockState.Locked;
    
    public Task Lock() => LockDoorAsync(null, default);
    public Task Unlock() => UnlockDoorAsync(null, default);
    public Task UnlockWithTimeout(int seconds) => UnlockWithTimeoutAsync((ushort)seconds, default);
    
    public void UpdateDoorState(DoorState state)
    {
        _doorState = state;
        OnDoorStateChanged?.Invoke(_doorState);
    }
}

public enum DoorLockState : byte
{
    NotFullyLocked = 0,
    Locked = 1,
    Unlocked = 2,
    Unlatched = 3
}

public enum DoorLockType : byte
{
    DeadBolt = 0,
    Magnetic = 1,
    Other = 2,
    Mortise = 3,
    Rim = 4,
    LatchBolt = 5,
    CylindricalLock = 6,
    TubularLock = 7,
    InterconnectedLock = 8,
    DeadLatch = 9,
    DoorFurniture = 10,
    Eurocylinder = 11
}

public enum DoorState : byte
{
    Open = 0,
    Closed = 1,
    Jammed = 2,
    ForcedOpen = 3,
    Unspecified = 4,
    Ajar = 5
}

public enum DoorLockOperatingMode : byte
{
    Normal = 0,
    Vacation = 1,
    Privacy = 2,
    NoRemoteLockUnlock = 3,
    Passage = 4
}

public enum DoorLockOperationEventCode : byte
{
    Unknown = 0,
    Lock = 1,
    Unlock = 2,
    LockInvalidPIN = 3,
    LockInvalidSchedule = 4,
    UnlockInvalidPIN = 5,
    UnlockInvalidSchedule = 6,
    OneTouchLock = 7,
    KeyLock = 8,
    KeyUnlock = 9,
    AutoLock = 10,
    ScheduleLock = 11,
    ScheduleUnlock = 12,
    ManualLock = 13,
    ManualUnlock = 14,
    InvalidCredential = 15
}
