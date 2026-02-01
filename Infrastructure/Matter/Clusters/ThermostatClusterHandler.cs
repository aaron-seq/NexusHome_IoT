using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Matter.Clusters;

/// <summary>
/// Matter Thermostat Cluster Handler (Cluster ID: 0x0201).
/// Controls HVAC systems including heating, cooling, and fan modes.
/// </summary>
public class ThermostatClusterHandler : ClusterHandlerBase
{
    public override uint ClusterId => 0x0201;
    public override string ClusterName => "Thermostat";

    // Attribute IDs
    private const uint ATTR_LOCAL_TEMPERATURE = 0x0000;
    private const uint ATTR_OUTDOOR_TEMPERATURE = 0x0001;
    private const uint ATTR_OCCUPIED_COOLING_SETPOINT = 0x0011;
    private const uint ATTR_OCCUPIED_HEATING_SETPOINT = 0x0012;
    private const uint ATTR_UNOCCUPIED_COOLING_SETPOINT = 0x0013;
    private const uint ATTR_UNOCCUPIED_HEATING_SETPOINT = 0x0014;
    private const uint ATTR_MIN_HEAT_SETPOINT_LIMIT = 0x0015;
    private const uint ATTR_MAX_HEAT_SETPOINT_LIMIT = 0x0016;
    private const uint ATTR_MIN_COOL_SETPOINT_LIMIT = 0x0017;
    private const uint ATTR_MAX_COOL_SETPOINT_LIMIT = 0x0018;
    private const uint ATTR_CONTROL_SEQUENCE_OF_OPERATION = 0x001B;
    private const uint ATTR_SYSTEM_MODE = 0x001C;
    private const uint ATTR_THERMOSTAT_RUNNING_MODE = 0x001E;
    private const uint ATTR_THERMOSTAT_RUNNING_STATE = 0x0029;

    // Command IDs
    private const uint CMD_SETPOINT_RAISE_LOWER = 0x00;
    private const uint CMD_SET_WEEKLY_SCHEDULE = 0x01;
    private const uint CMD_GET_WEEKLY_SCHEDULE = 0x02;
    private const uint CMD_CLEAR_WEEKLY_SCHEDULE = 0x03;

    // Current state (temperatures in 0.01 degrees Celsius)
    private short _localTemperature = 2200; // 22.00°C
    private short? _outdoorTemperature;
    private short _occupiedCoolingSetpoint = 2600; // 26.00°C
    private short _occupiedHeatingSetpoint = 2000; // 20.00°C
    private short _minHeatSetpointLimit = 700;  // 7.00°C
    private short _maxHeatSetpointLimit = 3000; // 30.00°C
    private short _minCoolSetpointLimit = 1600; // 16.00°C
    private short _maxCoolSetpointLimit = 3200; // 32.00°C
    private ThermostatSystemMode _systemMode = ThermostatSystemMode.Auto;
    private ThermostatRunningMode _runningMode = ThermostatRunningMode.Off;
    private ThermostatRunningState _runningState = ThermostatRunningState.Idle;

    // Events
    public event Action<short>? OnTemperatureChanged;
    public event Action<ThermostatSystemMode>? OnModeChanged;
    public event Action<short, short>? OnSetpointsChanged;

    public ThermostatClusterHandler(ILogger<ThermostatClusterHandler> logger) : base(logger)
    {
    }

    public override async Task<ClusterCommandResult> HandleCommandAsync(uint commandId, byte[]? payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Thermostat cluster handling command {CommandId}", commandId);

        switch (commandId)
        {
            case CMD_SETPOINT_RAISE_LOWER:
                if (payload != null && payload.Length >= 2)
                {
                    var mode = (SetpointAdjustMode)payload[0];
                    var amount = (sbyte)payload[1]; // in 0.1°C steps
                    await AdjustSetpoint(mode, amount);
                }
                return ClusterCommandResult.Ok();

            case CMD_CLEAR_WEEKLY_SCHEDULE:
                _logger.LogInformation("Weekly schedule cleared");
                return ClusterCommandResult.Ok();

            default:
                _logger.LogWarning("Unsupported Thermostat command: {CommandId}", commandId);
                return ClusterCommandResult.Fail($"Unsupported command: 0x{commandId:X2}", 0x81);
        }
    }

    private Task AdjustSetpoint(SetpointAdjustMode mode, sbyte amountTenths)
    {
        short adjustment = (short)(amountTenths * 10); // Convert to 0.01°C

        switch (mode)
        {
            case SetpointAdjustMode.Heat:
                _occupiedHeatingSetpoint = ClampSetpoint(_occupiedHeatingSetpoint + adjustment, _minHeatSetpointLimit, _maxHeatSetpointLimit);
                break;
            case SetpointAdjustMode.Cool:
                _occupiedCoolingSetpoint = ClampSetpoint(_occupiedCoolingSetpoint + adjustment, _minCoolSetpointLimit, _maxCoolSetpointLimit);
                break;
            case SetpointAdjustMode.Both:
                _occupiedHeatingSetpoint = ClampSetpoint(_occupiedHeatingSetpoint + adjustment, _minHeatSetpointLimit, _maxHeatSetpointLimit);
                _occupiedCoolingSetpoint = ClampSetpoint(_occupiedCoolingSetpoint + adjustment, _minCoolSetpointLimit, _maxCoolSetpointLimit);
                break;
        }

        OnSetpointsChanged?.Invoke(_occupiedHeatingSetpoint, _occupiedCoolingSetpoint);
        _logger.LogInformation("Setpoints adjusted: Heat={Heat}°C, Cool={Cool}°C", 
            _occupiedHeatingSetpoint / 100.0, _occupiedCoolingSetpoint / 100.0);

        return Task.CompletedTask;
    }

    private static short ClampSetpoint(int value, short min, short max) 
        => (short)Math.Clamp(value, min, max);

    public override Task<ClusterAttributeValue> ReadAttributeAsync(uint attributeId, CancellationToken cancellationToken = default)
    {
        ClusterAttributeValue result = attributeId switch
        {
            ATTR_LOCAL_TEMPERATURE => new() { AttributeId = attributeId, Value = _localTemperature, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_OUTDOOR_TEMPERATURE => new() { AttributeId = attributeId, Value = _outdoorTemperature, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_OCCUPIED_COOLING_SETPOINT => new() { AttributeId = attributeId, Value = _occupiedCoolingSetpoint, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_OCCUPIED_HEATING_SETPOINT => new() { AttributeId = attributeId, Value = _occupiedHeatingSetpoint, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_MIN_HEAT_SETPOINT_LIMIT => new() { AttributeId = attributeId, Value = _minHeatSetpointLimit, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_MAX_HEAT_SETPOINT_LIMIT => new() { AttributeId = attributeId, Value = _maxHeatSetpointLimit, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_MIN_COOL_SETPOINT_LIMIT => new() { AttributeId = attributeId, Value = _minCoolSetpointLimit, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_MAX_COOL_SETPOINT_LIMIT => new() { AttributeId = attributeId, Value = _maxCoolSetpointLimit, Type = ClusterAttributeType.Int16, Success = true },
            ATTR_SYSTEM_MODE => new() { AttributeId = attributeId, Value = (byte)_systemMode, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_THERMOSTAT_RUNNING_MODE => new() { AttributeId = attributeId, Value = (byte)_runningMode, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_THERMOSTAT_RUNNING_STATE => new() { AttributeId = attributeId, Value = (ushort)_runningState, Type = ClusterAttributeType.Bitmap16, Success = true },
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
                case ATTR_OCCUPIED_COOLING_SETPOINT:
                    _occupiedCoolingSetpoint = ClampSetpoint(Convert.ToInt16(value), _minCoolSetpointLimit, _maxCoolSetpointLimit);
                    OnSetpointsChanged?.Invoke(_occupiedHeatingSetpoint, _occupiedCoolingSetpoint);
                    return Task.FromResult(true);
                    
                case ATTR_OCCUPIED_HEATING_SETPOINT:
                    _occupiedHeatingSetpoint = ClampSetpoint(Convert.ToInt16(value), _minHeatSetpointLimit, _maxHeatSetpointLimit);
                    OnSetpointsChanged?.Invoke(_occupiedHeatingSetpoint, _occupiedCoolingSetpoint);
                    return Task.FromResult(true);
                    
                case ATTR_SYSTEM_MODE:
                    _systemMode = (ThermostatSystemMode)Convert.ToByte(value);
                    OnModeChanged?.Invoke(_systemMode);
                    _logger.LogInformation("Thermostat mode set to {Mode}", _systemMode);
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
        new() { AttributeId = ATTR_LOCAL_TEMPERATURE, Name = "LocalTemperature", Type = ClusterAttributeType.Int16, IsReportable = true },
        new() { AttributeId = ATTR_OCCUPIED_COOLING_SETPOINT, Name = "OccupiedCoolingSetpoint", Type = ClusterAttributeType.Int16, IsWritable = true, IsReportable = true },
        new() { AttributeId = ATTR_OCCUPIED_HEATING_SETPOINT, Name = "OccupiedHeatingSetpoint", Type = ClusterAttributeType.Int16, IsWritable = true, IsReportable = true },
        new() { AttributeId = ATTR_SYSTEM_MODE, Name = "SystemMode", Type = ClusterAttributeType.Enum8, IsWritable = true, IsReportable = true },
        new() { AttributeId = ATTR_THERMOSTAT_RUNNING_STATE, Name = "ThermostatRunningState", Type = ClusterAttributeType.Bitmap16, IsReportable = true },
    };

    public override IReadOnlyList<ClusterCommand> GetSupportedCommands() => new List<ClusterCommand>
    {
        new() { CommandId = CMD_SETPOINT_RAISE_LOWER, Name = "SetpointRaiseLower" },
        new() { CommandId = CMD_SET_WEEKLY_SCHEDULE, Name = "SetWeeklySchedule" },
        new() { CommandId = CMD_GET_WEEKLY_SCHEDULE, Name = "GetWeeklySchedule", RequiresResponse = true },
        new() { CommandId = CMD_CLEAR_WEEKLY_SCHEDULE, Name = "ClearWeeklySchedule" },
    };

    // Public API
    public decimal LocalTemperatureCelsius => _localTemperature / 100m;
    public decimal CoolingSetpointCelsius => _occupiedCoolingSetpoint / 100m;
    public decimal HeatingSetpointCelsius => _occupiedHeatingSetpoint / 100m;
    public ThermostatSystemMode SystemMode => _systemMode;
    
    public void SetCoolingSetpoint(decimal tempCelsius) 
        => WriteAttributeAsync(ATTR_OCCUPIED_COOLING_SETPOINT, (short)(tempCelsius * 100), default);
    
    public void SetHeatingSetpoint(decimal tempCelsius) 
        => WriteAttributeAsync(ATTR_OCCUPIED_HEATING_SETPOINT, (short)(tempCelsius * 100), default);
    
    public void SetMode(ThermostatSystemMode mode) 
        => WriteAttributeAsync(ATTR_SYSTEM_MODE, (byte)mode, default);
    
    public void UpdateLocalTemperature(decimal tempCelsius)
    {
        _localTemperature = (short)(tempCelsius * 100);
        OnTemperatureChanged?.Invoke(_localTemperature);
    }
}

public enum ThermostatSystemMode : byte
{
    Off = 0,
    Auto = 1,
    Cool = 3,
    Heat = 4,
    EmergencyHeat = 5,
    Precooling = 6,
    FanOnly = 7,
    Dry = 8,
    Sleep = 9
}

public enum ThermostatRunningMode : byte
{
    Off = 0,
    Cool = 3,
    Heat = 4
}

[Flags]
public enum ThermostatRunningState : ushort
{
    Idle = 0,
    HeatStateOn = 1,
    CoolStateOn = 2,
    FanStateOn = 4,
    HeatSecondStageStateOn = 8,
    CoolSecondStageStateOn = 16,
    FanSecondStageStateOn = 32,
    FanThirdStageStateOn = 64
}

public enum SetpointAdjustMode : byte
{
    Heat = 0,
    Cool = 1,
    Both = 2
}
