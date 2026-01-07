using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Core.DTOs;
using NexusHome.IoT.Core.Services;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Data;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace NexusHome.IoT.Tests.Unit.Services;

/// <summary>
/// Unit tests for PredictiveMaintenanceService
/// Tests ML-based maintenance prediction, anomaly detection, and health scoring
///
/// Design Rationale:
/// - Service uses ML.NET models for predictions, but current implementation is stubbed
/// - Tests verify the interface contract and error handling patterns
/// - Edge cases like null devices and invalid IDs are critical for production reliability
/// </summary>
public class PredictiveMaintenanceServiceTests : IDisposable
{
    private readonly Mock<ILogger<PredictiveMaintenanceService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ServiceProvider _serviceProvider;
    private readonly SmartHomeDbContext _dbContext;

    public PredictiveMaintenanceServiceTests()
    {
        _mockLogger = new Mock<ILogger<PredictiveMaintenanceService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup in-memory database
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<SmartHomeDbContext>(options =>
            options.UseInMemoryDatabase($"MaintenanceTestDb_{Guid.NewGuid()}"));

        _serviceProvider = serviceCollection.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<SmartHomeDbContext>();
    }

    #region PredictMaintenanceNeedsAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task PredictMaintenanceNeedsAsync_WithValidDevice_ShouldReturnPrediction()
    {
        // Arrange
        var device = await SeedTestDevice();
        var service = CreateService();

        // Act
        var result = await service.PredictMaintenanceNeedsAsync(device.Id);

        // Assert
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(device.Id);
        result.DeviceName.Should().Be(device.DeviceFriendlyName);
        result.FailureProbability.Should().BeInRange(0.0, 1.0);
        result.Confidence.Should().BeInRange(0.0, 1.0);
        result.Recommendation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task PredictMaintenanceNeedsAsync_WithNonExistentDevice_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();
        var nonExistentDeviceId = 99999;

        // Act
        var act = async () => await service.PredictMaintenanceNeedsAsync(nonExistentDeviceId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{nonExistentDeviceId}*not found*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task PredictMaintenanceNeedsAsync_WhenErrorOccurs_ShouldLogAndRethrow()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();

        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(s => s.ServiceProvider).Throws(new InvalidOperationException("Test exception"));
        mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);

        var service = new PredictiveMaintenanceService(
            mockServiceProvider.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var act = async () => await service.PredictMaintenanceNeedsAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in maintenance prediction")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DetectAnomaliesAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task DetectAnomaliesAsync_WithValidDevice_ShouldReturnResult()
    {
        // Arrange
        var device = await SeedTestDevice();
        var service = CreateService();

        // Act
        var result = await service.DetectAnomaliesAsync(device.Id);

        // Assert
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(device.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task DetectAnomaliesAsync_WithVariousDeviceIds_ShouldNotThrow(int deviceId)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DetectAnomaliesAsync(deviceId);

        // Assert - Current implementation returns result for any ID
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(deviceId);
    }

    #endregion

    #region CalculateDeviceHealthScoreAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task CalculateDeviceHealthScoreAsync_WithValidDevice_ShouldReturnHealthScore()
    {
        // Arrange
        var device = await SeedTestDevice();
        var service = CreateService();

        // Act
        var result = await service.CalculateDeviceHealthScoreAsync(device.Id);

        // Assert
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(device.Id);
        result.HealthScore.Should().BeInRange(0, 100);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task CalculateDeviceHealthScoreAsync_ShouldReturnConsistentScores()
    {
        // Arrange
        var device = await SeedTestDevice();
        var service = CreateService();

        // Act
        var result1 = await service.CalculateDeviceHealthScoreAsync(device.Id);
        var result2 = await service.CalculateDeviceHealthScoreAsync(device.Id);

        // Assert - Same device should return consistent scores
        result1.HealthScore.Should().Be(result2.HealthScore);
    }

    #endregion

    #region TrainModelAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task TrainModelAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.TrainModelAsync("Thermostat");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Lighting")]
    [InlineData("ClimateControl")]
    [InlineData("SecurityCamera")]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    [Trait("Component", "PredictiveMaintenance")]
    public async Task TrainModelAsync_WithVariousDeviceTypes_ShouldNotThrow(string deviceType)
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.TrainModelAsync(deviceType);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private PredictiveMaintenanceService CreateService()
    {
        return new PredictiveMaintenanceService(
            _serviceProvider,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    private async Task<SmartDevice> SeedTestDevice()
    {
        var device = new SmartDevice
        {
            UniqueDeviceIdentifier = $"test-device-{Guid.NewGuid():N}"[..20],
            DeviceFriendlyName = "Test Smart Thermostat",
            DeviceDescription = "Test device for maintenance prediction",
            DeviceType = DeviceCategory.ClimateControl,
            ManufacturerName = "TestCorp",
            ModelNumber = "TC-THERMO-001",
            IsCurrentlyOnline = true,
            CurrentStatus = DeviceOperationalStatus.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastCommunicationTime = DateTime.UtcNow.AddMinutes(-5)
        };

        await _dbContext.SmartDevices.AddAsync(device);
        await _dbContext.SaveChangesAsync();
        return device;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }

    #endregion
}
