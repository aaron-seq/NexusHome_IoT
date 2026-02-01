using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NexusHome.IoT.Infrastructure.Matter.Clusters;
using Xunit;

namespace NexusHome.IoT.Tests.Unit.Matter;

/// <summary>
/// Unit tests for Matter cluster handlers.
/// </summary>
public class OnOffClusterHandlerTests
{
    private readonly Mock<ILogger<OnOffClusterHandler>> _loggerMock;
    private readonly OnOffClusterHandler _handler;

    public OnOffClusterHandlerTests()
    {
        _loggerMock = new Mock<ILogger<OnOffClusterHandler>>();
        _handler = new OnOffClusterHandler(_loggerMock.Object);
    }

    [Fact]
    public void ClusterId_ShouldReturn0x0006()
    {
        _handler.ClusterId.Should().Be(0x0006);
    }

    [Fact]
    public void ClusterName_ShouldReturnOnOff()
    {
        _handler.ClusterName.Should().Be("OnOff");
    }

    [Fact]
    public async Task HandleCommandAsync_On_ShouldTurnDeviceOn()
    {
        // Arrange
        const uint cmdOn = 0x01;
        bool stateChanged = false;
        bool newState = false;
        _handler.OnStateChanged += state => { stateChanged = true; newState = state; };

        // Act
        var result = await _handler.HandleCommandAsync(cmdOn, null);

        // Assert
        result.Success.Should().BeTrue();
        _handler.IsOn.Should().BeTrue();
        stateChanged.Should().BeTrue();
        newState.Should().BeTrue();
    }

    [Fact]
    public async Task HandleCommandAsync_Off_ShouldTurnDeviceOff()
    {
        // Arrange - first turn on
        await _handler.HandleCommandAsync(0x01, null);
        const uint cmdOff = 0x00;

        // Act
        var result = await _handler.HandleCommandAsync(cmdOff, null);

        // Assert
        result.Success.Should().BeTrue();
        _handler.IsOn.Should().BeFalse();
    }

    [Fact]
    public async Task HandleCommandAsync_Toggle_ShouldInvertState()
    {
        // Arrange
        const uint cmdToggle = 0x02;
        _handler.IsOn.Should().BeFalse();

        // Act
        await _handler.HandleCommandAsync(cmdToggle, null);

        // Assert
        _handler.IsOn.Should().BeTrue();

        // Toggle again
        await _handler.HandleCommandAsync(cmdToggle, null);
        _handler.IsOn.Should().BeFalse();
    }

    [Fact]
    public async Task HandleCommandAsync_InvalidCommand_ShouldFail()
    {
        // Arrange
        const uint invalidCmd = 0xFF;

        // Act
        var result = await _handler.HandleCommandAsync(invalidCmd, null);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(0x81);
    }

    [Fact]
    public async Task ReadAttributeAsync_OnOff_ShouldReturnCurrentState()
    {
        // Arrange
        await _handler.HandleCommandAsync(0x01, null); // Turn on

        // Act
        var result = await _handler.ReadAttributeAsync(0x0000);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().Be(true);
        result.Type.Should().Be(ClusterAttributeType.Boolean);
    }

    [Fact]
    public async Task ReadAttributeAsync_Unknown_ShouldFail()
    {
        // Act
        var result = await _handler.ReadAttributeAsync(0xFFFF);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown attribute");
    }

    [Fact]
    public void GetSupportedAttributes_ShouldReturnExpectedAttributes()
    {
        // Act
        var attributes = _handler.GetSupportedAttributes();

        // Assert
        attributes.Should().HaveCountGreaterOrEqualTo(4);
        attributes.Should().Contain(a => a.Name == "OnOff");
    }

    [Fact]
    public void GetSupportedCommands_ShouldReturnExpectedCommands()
    {
        // Act
        var commands = _handler.GetSupportedCommands();

        // Assert
        commands.Should().HaveCountGreaterOrEqualTo(3);
        commands.Should().Contain(c => c.Name == "On");
        commands.Should().Contain(c => c.Name == "Off");
        commands.Should().Contain(c => c.Name == "Toggle");
    }
}

public class LevelControlClusterHandlerTests
{
    private readonly Mock<ILogger<LevelControlClusterHandler>> _loggerMock;
    private readonly LevelControlClusterHandler _handler;

    public LevelControlClusterHandlerTests()
    {
        _loggerMock = new Mock<ILogger<LevelControlClusterHandler>>();
        _handler = new LevelControlClusterHandler(_loggerMock.Object);
    }

    [Fact]
    public void ClusterId_ShouldReturn0x0008()
    {
        _handler.ClusterId.Should().Be(0x0008);
    }

    [Fact]
    public async Task HandleCommandAsync_MoveToLevel_ShouldSetLevel()
    {
        // Arrange
        const uint cmdMoveToLevel = 0x00;
        byte targetLevel = 127;
        ushort transitionTime = 10;
        var payload = new byte[] { targetLevel, (byte)(transitionTime & 0xFF), (byte)(transitionTime >> 8) };
        
        bool levelChanged = false;
        byte newLevel = 0;
        _handler.OnLevelChanged += level => { levelChanged = true; newLevel = level; };

        // Act
        var result = await _handler.HandleCommandAsync(cmdMoveToLevel, payload);

        // Assert
        result.Success.Should().BeTrue();
        _handler.CurrentLevel.Should().Be(127);
        levelChanged.Should().BeTrue();
        newLevel.Should().Be(127);
    }

    [Fact]
    public async Task HandleCommandAsync_MoveToLevel_ShouldClampToMax()
    {
        // Arrange
        const uint cmdMoveToLevel = 0x00;
        var payload = new byte[] { 255, 0, 0 }; // Level 255, transition 0

        // Act
        await _handler.HandleCommandAsync(cmdMoveToLevel, payload);

        // Assert
        _handler.CurrentLevel.Should().BeLessOrEqualTo(_handler.MaxLevel);
    }

    [Fact]
    public void SetLevelPercent_ShouldConvertCorrectly()
    {
        // Act
        _handler.SetLevelPercent(50);

        // Assert
        _handler.GetLevelPercent().Should().BeInRange(49, 51);
    }

    [Fact]
    public async Task HandleCommandAsync_Step_ShouldIncrementLevel()
    {
        // Arrange
        _handler.SetLevel(100);
        const uint cmdStep = 0x02;
        var payload = new byte[] { 0x00, 10, 0, 0 }; // StepMode.Up, stepSize=10

        // Act
        await _handler.HandleCommandAsync(cmdStep, payload);

        // Assert
        _handler.CurrentLevel.Should().Be(110);
    }
}

public class ThermostatClusterHandlerTests
{
    private readonly Mock<ILogger<ThermostatClusterHandler>> _loggerMock;
    private readonly ThermostatClusterHandler _handler;

    public ThermostatClusterHandlerTests()
    {
        _loggerMock = new Mock<ILogger<ThermostatClusterHandler>>();
        _handler = new ThermostatClusterHandler(_loggerMock.Object);
    }

    [Fact]
    public void ClusterId_ShouldReturn0x0201()
    {
        _handler.ClusterId.Should().Be(0x0201);
    }

    [Fact]
    public void LocalTemperatureCelsius_ShouldReturnDefaultTemperature()
    {
        _handler.LocalTemperatureCelsius.Should().Be(22.0m);
    }

    [Fact]
    public void SetCoolingSetpoint_ShouldUpdateSetpoint()
    {
        // Act
        _handler.SetCoolingSetpoint(25.5m);

        // Assert
        _handler.CoolingSetpointCelsius.Should().BeInRange(25.4m, 25.6m);
    }

    [Fact]
    public void SetMode_ShouldUpdateSystemMode()
    {
        // Arrange
        bool modeChanged = false;
        ThermostatSystemMode newMode = ThermostatSystemMode.Off;
        _handler.OnModeChanged += mode => { modeChanged = true; newMode = mode; };

        // Act
        _handler.SetMode(ThermostatSystemMode.Cool);

        // Assert
        _handler.SystemMode.Should().Be(ThermostatSystemMode.Cool);
        modeChanged.Should().BeTrue();
        newMode.Should().Be(ThermostatSystemMode.Cool);
    }

    [Fact]
    public async Task ReadAttributeAsync_LocalTemperature_ShouldReturnValue()
    {
        // Act
        var result = await _handler.ReadAttributeAsync(0x0000);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().Be((short)2200); // 22.00°C in 0.01°C units
    }
}

public class DoorLockClusterHandlerTests
{
    private readonly Mock<ILogger<DoorLockClusterHandler>> _loggerMock;
    private readonly DoorLockClusterHandler _handler;

    public DoorLockClusterHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DoorLockClusterHandler>>();
        _handler = new DoorLockClusterHandler(_loggerMock.Object);
    }

    [Fact]
    public void ClusterId_ShouldReturn0x0101()
    {
        _handler.ClusterId.Should().Be(0x0101);
    }

    [Fact]
    public async Task Lock_ShouldSetLockedState()
    {
        // Arrange
        _handler.IsLocked.Should().BeFalse();

        // Act
        await _handler.Lock();

        // Assert
        _handler.IsLocked.Should().BeTrue();
        _handler.LockState.Should().Be(DoorLockState.Locked);
    }

    [Fact]
    public async Task Unlock_ShouldSetUnlockedState()
    {
        // Arrange
        await _handler.Lock();

        // Act
        await _handler.Unlock();

        // Assert
        _handler.IsLocked.Should().BeFalse();
        _handler.LockState.Should().Be(DoorLockState.Unlocked);
    }

    [Fact]
    public void UpdateDoorState_ShouldTriggerEvent()
    {
        // Arrange
        bool stateChanged = false;
        DoorState newState = DoorState.Closed;
        _handler.OnDoorStateChanged += state => { stateChanged = true; newState = state; };

        // Act
        _handler.UpdateDoorState(DoorState.Open);

        // Assert
        stateChanged.Should().BeTrue();
        newState.Should().Be(DoorState.Open);
        _handler.DoorState.Should().Be(DoorState.Open);
    }
}

public class ColorControlClusterHandlerTests
{
    private readonly Mock<ILogger<ColorControlClusterHandler>> _loggerMock;
    private readonly ColorControlClusterHandler _handler;

    public ColorControlClusterHandlerTests()
    {
        _loggerMock = new Mock<ILogger<ColorControlClusterHandler>>();
        _handler = new ColorControlClusterHandler(_loggerMock.Object);
    }

    [Fact]
    public void ClusterId_ShouldReturn0x0300()
    {
        _handler.ClusterId.Should().Be(0x0300);
    }

    [Fact]
    public void SetRgb_ShouldUpdateHueSaturation()
    {
        // Arrange
        bool hsChanged = false;
        _handler.OnHueSaturationChanged += (h, s) => hsChanged = true;

        // Act - Set to red
        _handler.SetRgb(255, 0, 0);

        // Assert
        hsChanged.Should().BeTrue();
        _handler.CurrentHue.Should().BeInRange(0, 10); // Red is around hue 0
        _handler.CurrentSaturation.Should().BeGreaterThan(200);
    }

    [Fact]
    public void SetColorTemperature_ShouldUpdateMireds()
    {
        // Arrange
        bool tempChanged = false;
        _handler.OnColorTemperatureChanged += temp => tempChanged = true;

        // Act
        _handler.SetColorTemperature(4000);

        // Assert
        tempChanged.Should().BeTrue();
        _handler.ColorTemperatureKelvin.Should().BeInRange(3900, 4100);
    }

    [Fact]
    public void GetRgb_AfterSetRgb_ShouldApproximateOriginal()
    {
        // Arrange
        _handler.SetRgb(200, 100, 50);

        // Act
        var (r, g, b) = _handler.GetRgb();

        // Assert - Allow some variance due to color space conversion
        r.Should().BeInRange(180, 255);
        g.Should().BeInRange(80, 120);
        b.Should().BeInRange(30, 80);
    }

    [Fact]
    public async Task HandleCommandAsync_MoveToColorTemperature_ShouldClampToLimits()
    {
        // Arrange
        const uint cmdMoveToColorTemp = 0x0A;
        var payload = new byte[] { 100, 0, 0, 0 }; // 100 mireds (10000K) - below min

        // Act
        await _handler.HandleCommandAsync(cmdMoveToColorTemp, payload);

        // Assert - Should be clamped to physical minimum (153 mireds = ~6500K)
        _handler.ColorTemperatureKelvin.Should().BeGreaterOrEqualTo(6000);
    }
}
