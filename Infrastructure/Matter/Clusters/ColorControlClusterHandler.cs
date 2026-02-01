using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Matter.Clusters;

/// <summary>
/// Matter Color Control Cluster Handler (Cluster ID: 0x0300).
/// Controls RGB lights with hue, saturation, and color temperature.
/// </summary>
public class ColorControlClusterHandler : ClusterHandlerBase
{
    public override uint ClusterId => 0x0300;
    public override string ClusterName => "ColorControl";

    // Attribute IDs
    private const uint ATTR_CURRENT_HUE = 0x0000;
    private const uint ATTR_CURRENT_SATURATION = 0x0001;
    private const uint ATTR_REMAINING_TIME = 0x0002;
    private const uint ATTR_CURRENT_X = 0x0003;
    private const uint ATTR_CURRENT_Y = 0x0004;
    private const uint ATTR_COLOR_TEMPERATURE_MIREDS = 0x0007;
    private const uint ATTR_COLOR_MODE = 0x0008;
    private const uint ATTR_ENHANCED_CURRENT_HUE = 0x4000;
    private const uint ATTR_ENHANCED_COLOR_MODE = 0x4001;
    private const uint ATTR_COLOR_LOOP_ACTIVE = 0x4002;
    private const uint ATTR_COLOR_CAPABILITIES = 0x400A;
    private const uint ATTR_COLOR_TEMP_PHYSICAL_MIN_MIREDS = 0x400B;
    private const uint ATTR_COLOR_TEMP_PHYSICAL_MAX_MIREDS = 0x400C;

    // Command IDs
    private const uint CMD_MOVE_TO_HUE = 0x00;
    private const uint CMD_MOVE_HUE = 0x01;
    private const uint CMD_STEP_HUE = 0x02;
    private const uint CMD_MOVE_TO_SATURATION = 0x03;
    private const uint CMD_MOVE_SATURATION = 0x04;
    private const uint CMD_STEP_SATURATION = 0x05;
    private const uint CMD_MOVE_TO_HUE_AND_SATURATION = 0x06;
    private const uint CMD_MOVE_TO_COLOR = 0x07;
    private const uint CMD_MOVE_COLOR = 0x08;
    private const uint CMD_STEP_COLOR = 0x09;
    private const uint CMD_MOVE_TO_COLOR_TEMPERATURE = 0x0A;
    private const uint CMD_ENHANCED_MOVE_TO_HUE = 0x40;
    private const uint CMD_ENHANCED_MOVE_TO_HUE_AND_SATURATION = 0x43;
    private const uint CMD_COLOR_LOOP_SET = 0x44;
    private const uint CMD_STOP_MOVE_STEP = 0x47;

    // Current state
    private byte _currentHue = 0;
    private byte _currentSaturation = 0;
    private ushort _remainingTime = 0;
    private ushort _currentX = 0x616B; // D65 white point X
    private ushort _currentY = 0x607D; // D65 white point Y
    private ushort _colorTemperatureMireds = 250; // 4000K
    private ColorMode _colorMode = ColorMode.ColorTemperature;
    private ushort _enhancedCurrentHue = 0;
    private bool _colorLoopActive = false;
    private ushort _colorTempPhysicalMinMireds = 153; // ~6500K
    private ushort _colorTempPhysicalMaxMireds = 500; // ~2000K

    // Events
    public event Action<byte, byte>? OnHueSaturationChanged;
    public event Action<ushort, ushort>? OnXYChanged;
    public event Action<ushort>? OnColorTemperatureChanged;

    public ColorControlClusterHandler(ILogger<ColorControlClusterHandler> logger) : base(logger)
    {
    }

    public override async Task<ClusterCommandResult> HandleCommandAsync(uint commandId, byte[]? payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ColorControl cluster handling command {CommandId}", commandId);

        switch (commandId)
        {
            case CMD_MOVE_TO_HUE:
                if (payload != null && payload.Length >= 4)
                {
                    var hue = payload[0];
                    var direction = (HueDirection)payload[1];
                    var transitionTime = BitConverter.ToUInt16(payload, 2);
                    await MoveToHueAsync(hue, direction, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_MOVE_TO_SATURATION:
                if (payload != null && payload.Length >= 3)
                {
                    var saturation = payload[0];
                    var transitionTime = BitConverter.ToUInt16(payload, 1);
                    await MoveToSaturationAsync(saturation, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_MOVE_TO_HUE_AND_SATURATION:
                if (payload != null && payload.Length >= 4)
                {
                    var hue = payload[0];
                    var saturation = payload[1];
                    var transitionTime = BitConverter.ToUInt16(payload, 2);
                    await MoveToHueAndSaturationAsync(hue, saturation, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_MOVE_TO_COLOR:
                if (payload != null && payload.Length >= 6)
                {
                    var colorX = BitConverter.ToUInt16(payload, 0);
                    var colorY = BitConverter.ToUInt16(payload, 2);
                    var transitionTime = BitConverter.ToUInt16(payload, 4);
                    await MoveToColorAsync(colorX, colorY, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_MOVE_TO_COLOR_TEMPERATURE:
                if (payload != null && payload.Length >= 4)
                {
                    var colorTempMireds = BitConverter.ToUInt16(payload, 0);
                    var transitionTime = BitConverter.ToUInt16(payload, 2);
                    await MoveToColorTemperatureAsync(colorTempMireds, transitionTime, cancellationToken);
                }
                return ClusterCommandResult.Ok();

            case CMD_STOP_MOVE_STEP:
                _remainingTime = 0;
                _logger.LogInformation("Color transition stopped");
                return ClusterCommandResult.Ok();

            default:
                _logger.LogWarning("Unsupported ColorControl command: {CommandId}", commandId);
                return ClusterCommandResult.Fail($"Unsupported command: 0x{commandId:X2}", 0x81);
        }
    }

    private Task MoveToHueAsync(byte hue, HueDirection direction, ushort transitionTime, CancellationToken cancellationToken)
    {
        _currentHue = hue;
        _colorMode = ColorMode.HueSaturation;
        _remainingTime = 0;
        OnHueSaturationChanged?.Invoke(_currentHue, _currentSaturation);
        
        _logger.LogInformation("Hue set to {Hue} ({Direction}, transition: {TransitionTime}ms)", 
            _currentHue, direction, transitionTime * 100);
        
        return Task.CompletedTask;
    }

    private Task MoveToSaturationAsync(byte saturation, ushort transitionTime, CancellationToken cancellationToken)
    {
        _currentSaturation = saturation;
        _colorMode = ColorMode.HueSaturation;
        _remainingTime = 0;
        OnHueSaturationChanged?.Invoke(_currentHue, _currentSaturation);
        
        _logger.LogInformation("Saturation set to {Saturation} (transition: {TransitionTime}ms)", 
            _currentSaturation, transitionTime * 100);
        
        return Task.CompletedTask;
    }

    private Task MoveToHueAndSaturationAsync(byte hue, byte saturation, ushort transitionTime, CancellationToken cancellationToken)
    {
        _currentHue = hue;
        _currentSaturation = saturation;
        _colorMode = ColorMode.HueSaturation;
        _remainingTime = 0;
        OnHueSaturationChanged?.Invoke(_currentHue, _currentSaturation);
        
        _logger.LogInformation("Hue/Saturation set to {Hue}/{Saturation} (transition: {TransitionTime}ms)", 
            _currentHue, _currentSaturation, transitionTime * 100);
        
        return Task.CompletedTask;
    }

    private Task MoveToColorAsync(ushort colorX, ushort colorY, ushort transitionTime, CancellationToken cancellationToken)
    {
        _currentX = colorX;
        _currentY = colorY;
        _colorMode = ColorMode.CurrentXY;
        _remainingTime = 0;
        OnXYChanged?.Invoke(_currentX, _currentY);
        
        _logger.LogInformation("Color XY set to ({X}, {Y}) (transition: {TransitionTime}ms)", 
            _currentX / 65535.0, _currentY / 65535.0, transitionTime * 100);
        
        return Task.CompletedTask;
    }

    private Task MoveToColorTemperatureAsync(ushort colorTempMireds, ushort transitionTime, CancellationToken cancellationToken)
    {
        _colorTemperatureMireds = Math.Clamp(colorTempMireds, _colorTempPhysicalMinMireds, _colorTempPhysicalMaxMireds);
        _colorMode = ColorMode.ColorTemperature;
        _remainingTime = 0;
        OnColorTemperatureChanged?.Invoke(_colorTemperatureMireds);
        
        var kelvin = 1000000 / _colorTemperatureMireds;
        _logger.LogInformation("Color temperature set to {Mireds} mireds ({Kelvin}K) (transition: {TransitionTime}ms)", 
            _colorTemperatureMireds, kelvin, transitionTime * 100);
        
        return Task.CompletedTask;
    }

    public override Task<ClusterAttributeValue> ReadAttributeAsync(uint attributeId, CancellationToken cancellationToken = default)
    {
        ClusterAttributeValue result = attributeId switch
        {
            ATTR_CURRENT_HUE => new() { AttributeId = attributeId, Value = _currentHue, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_CURRENT_SATURATION => new() { AttributeId = attributeId, Value = _currentSaturation, Type = ClusterAttributeType.UInt8, Success = true },
            ATTR_REMAINING_TIME => new() { AttributeId = attributeId, Value = _remainingTime, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_CURRENT_X => new() { AttributeId = attributeId, Value = _currentX, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_CURRENT_Y => new() { AttributeId = attributeId, Value = _currentY, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_COLOR_TEMPERATURE_MIREDS => new() { AttributeId = attributeId, Value = _colorTemperatureMireds, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_COLOR_MODE => new() { AttributeId = attributeId, Value = (byte)_colorMode, Type = ClusterAttributeType.Enum8, Success = true },
            ATTR_ENHANCED_CURRENT_HUE => new() { AttributeId = attributeId, Value = _enhancedCurrentHue, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_COLOR_LOOP_ACTIVE => new() { AttributeId = attributeId, Value = _colorLoopActive, Type = ClusterAttributeType.Boolean, Success = true },
            ATTR_COLOR_TEMP_PHYSICAL_MIN_MIREDS => new() { AttributeId = attributeId, Value = _colorTempPhysicalMinMireds, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_COLOR_TEMP_PHYSICAL_MAX_MIREDS => new() { AttributeId = attributeId, Value = _colorTempPhysicalMaxMireds, Type = ClusterAttributeType.UInt16, Success = true },
            ATTR_COLOR_CAPABILITIES => new() { AttributeId = attributeId, Value = (ushort)(ColorCapabilities.HueSaturation | ColorCapabilities.XY | ColorCapabilities.ColorTemperature), Type = ClusterAttributeType.Bitmap16, Success = true },
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
                case ATTR_COLOR_TEMPERATURE_MIREDS:
                    _colorTemperatureMireds = Math.Clamp(Convert.ToUInt16(value), _colorTempPhysicalMinMireds, _colorTempPhysicalMaxMireds);
                    _colorMode = ColorMode.ColorTemperature;
                    OnColorTemperatureChanged?.Invoke(_colorTemperatureMireds);
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
        new() { AttributeId = ATTR_CURRENT_HUE, Name = "CurrentHue", Type = ClusterAttributeType.UInt8, IsReportable = true },
        new() { AttributeId = ATTR_CURRENT_SATURATION, Name = "CurrentSaturation", Type = ClusterAttributeType.UInt8, IsReportable = true },
        new() { AttributeId = ATTR_REMAINING_TIME, Name = "RemainingTime", Type = ClusterAttributeType.UInt16 },
        new() { AttributeId = ATTR_CURRENT_X, Name = "CurrentX", Type = ClusterAttributeType.UInt16, IsReportable = true },
        new() { AttributeId = ATTR_CURRENT_Y, Name = "CurrentY", Type = ClusterAttributeType.UInt16, IsReportable = true },
        new() { AttributeId = ATTR_COLOR_TEMPERATURE_MIREDS, Name = "ColorTemperatureMireds", Type = ClusterAttributeType.UInt16, IsWritable = true, IsReportable = true },
        new() { AttributeId = ATTR_COLOR_MODE, Name = "ColorMode", Type = ClusterAttributeType.Enum8, IsReportable = true },
        new() { AttributeId = ATTR_COLOR_CAPABILITIES, Name = "ColorCapabilities", Type = ClusterAttributeType.Bitmap16 },
    };

    public override IReadOnlyList<ClusterCommand> GetSupportedCommands() => new List<ClusterCommand>
    {
        new() { CommandId = CMD_MOVE_TO_HUE, Name = "MoveToHue" },
        new() { CommandId = CMD_MOVE_TO_SATURATION, Name = "MoveToSaturation" },
        new() { CommandId = CMD_MOVE_TO_HUE_AND_SATURATION, Name = "MoveToHueAndSaturation" },
        new() { CommandId = CMD_MOVE_TO_COLOR, Name = "MoveToColor" },
        new() { CommandId = CMD_MOVE_TO_COLOR_TEMPERATURE, Name = "MoveToColorTemperature" },
        new() { CommandId = CMD_STOP_MOVE_STEP, Name = "StopMoveStep" },
    };

    // Public API
    public byte CurrentHue => _currentHue;
    public byte CurrentSaturation => _currentSaturation;
    public ushort ColorTemperatureKelvin => (ushort)(1000000 / _colorTemperatureMireds);
    public ColorMode CurrentColorMode => _colorMode;
    
    // RGB conversion helpers
    public (byte R, byte G, byte B) GetRgb()
    {
        return _colorMode switch
        {
            ColorMode.HueSaturation => HsvToRgb(_currentHue * 360 / 254.0, _currentSaturation / 254.0, 1.0),
            ColorMode.EnhancedHueSaturation => HsvToRgb(_enhancedCurrentHue * 360 / 65535.0, _currentSaturation / 254.0, 1.0),
            ColorMode.CurrentXY => XyToRgb(_currentX / 65535.0, _currentY / 65535.0),
            ColorMode.ColorTemperature => KelvinToRgb(1000000 / _colorTemperatureMireds),
            _ => KelvinToRgb(1000000 / _colorTemperatureMireds)
        };
    }
    
    public void SetRgb(byte r, byte g, byte b)
    {
        var (h, s, v) = RgbToHsv(r, g, b);
        _currentHue = (byte)(h * 254 / 360);
        _currentSaturation = (byte)(s * 254);
        _colorMode = ColorMode.HueSaturation;
        OnHueSaturationChanged?.Invoke(_currentHue, _currentSaturation);
    }
    
    public void SetColorTemperature(int kelvin)
    {
        var mireds = (ushort)(1000000 / Math.Clamp(kelvin, 2000, 6500));
        MoveToColorTemperatureAsync(mireds, 0, default);
    }

    // Color conversion utilities
    private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        
        var (r1, g1, b1) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };
        
        return ((byte)((r1 + m) * 255), (byte)((g1 + m) * 255), (byte)((b1 + m) * 255));
    }
    
    private static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
    {
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;
        
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;
        
        double h = 0;
        if (delta > 0)
        {
            if (max == rf) h = 60 * ((gf - bf) / delta % 6);
            else if (max == gf) h = 60 * ((bf - rf) / delta + 2);
            else h = 60 * ((rf - gf) / delta + 4);
        }
        if (h < 0) h += 360;
        
        var s = max > 0 ? delta / max : 0;
        return (h, s, max);
    }
    
    private static (byte R, byte G, byte B) XyToRgb(double x, double y)
    {
        // Simplified CIE XY to RGB conversion
        var z = 1.0 - x - y;
        var Y = 1.0;
        var X = (Y / y) * x;
        var Z = (Y / y) * z;
        
        var r = X * 3.2406 - Y * 1.5372 - Z * 0.4986;
        var g = -X * 0.9689 + Y * 1.8758 + Z * 0.0415;
        var b = X * 0.0557 - Y * 0.2040 + Z * 1.0570;
        
        return (
            (byte)Math.Clamp(r * 255, 0, 255),
            (byte)Math.Clamp(g * 255, 0, 255),
            (byte)Math.Clamp(b * 255, 0, 255)
        );
    }
    
    private static (byte R, byte G, byte B) KelvinToRgb(int kelvin)
    {
        var temp = kelvin / 100.0;
        double r, g, b;
        
        if (temp <= 66)
        {
            r = 255;
            g = 99.4708025861 * Math.Log(temp) - 161.1195681661;
            b = temp <= 19 ? 0 : 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;
        }
        else
        {
            r = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
            g = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
            b = 255;
        }
        
        return (
            (byte)Math.Clamp(r, 0, 255),
            (byte)Math.Clamp(g, 0, 255),
            (byte)Math.Clamp(b, 0, 255)
        );
    }
}

public enum ColorMode : byte
{
    HueSaturation = 0,
    CurrentXY = 1,
    ColorTemperature = 2,
    EnhancedHueSaturation = 3
}

public enum HueDirection : byte
{
    ShortestDistance = 0,
    LongestDistance = 1,
    Up = 2,
    Down = 3
}

[Flags]
public enum ColorCapabilities : ushort
{
    HueSaturation = 1,
    EnhancedHue = 2,
    ColorLoop = 4,
    XY = 8,
    ColorTemperature = 16
}
