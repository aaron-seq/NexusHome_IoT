using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Core.Domain;
using System.Security.Cryptography;

namespace NexusHome.IoT.Infrastructure.Services;

public class DeviceDataCollectionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceDataCollectionService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public DeviceDataCollectionService(
        IServiceProvider serviceProvider,
        ILogger<DeviceDataCollectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Data Collection Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

                // Simulate data collection for online devices
                var devices = await context.SmartDevices
                    .Where(d => d.IsCurrentlyOnline)
                    .ToListAsync();

                foreach (var device in devices)
                {
                    await CollectDeviceDataAsync(context, device);
                }
                
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Device Data Collection Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CollectDeviceDataAsync(SmartHomeDbContext context, SmartHomeDevice device)
    {
        // Simulate reading from device
        // In real world, this would call device API or check MQTT buffer
        
        var random = RandomNumberGenerator.GetInt32(0, 100);
        decimal consumption = device.CurrentPowerConsumption + (decimal)(random - 50) / 100;
        if (consumption < 0) consumption = 0;
        
        device.CurrentPowerConsumption = consumption;
        device.LastCommunicationTime = DateTime.UtcNow;

        // Record history
        context.EnergyConsumptions.Add(new DeviceEnergyConsumption
        {
            SmartHomeDeviceId = device.Id,
            PowerConsumptionKilowattHours = consumption / 1000, // Instantaneous mock
            MeasurementTimestamp = DateTime.UtcNow,
            VoltageReading = 220 + (decimal)(random % 5),
            CurrentReading = consumption / 220,
            FrequencyReading = 50,
            PowerFactorReading = 0.95m
        });
    }
}
