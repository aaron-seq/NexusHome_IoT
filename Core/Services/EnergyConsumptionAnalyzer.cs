using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Core.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace NexusHome.IoT.Core.Services;

public class EnergyConsumptionAnalyzer : IEnergyConsumptionAnalyzer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnergyConsumptionAnalyzer> _logger;

    public EnergyConsumptionAnalyzer(
        IServiceProvider serviceProvider,
        ILogger<EnergyConsumptionAnalyzer> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<decimal> GetCurrentConsumptionAsync()
    {
        // Snapshot of current total power usage (e.g., from last minute)
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var consumption = await context.SmartDevices
            .SumAsync(d => d.CurrentPowerConsumption);

        return consumption;
    }

    public async Task<decimal> GetDailyConsumptionAsync(DateTime date)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
        
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var total = await context.DeviceEnergyConsumptions
            .Where(e => e.MeasurementTimestamp >= startOfDay && e.MeasurementTimestamp < endOfDay)
            .SumAsync(e => e.PowerConsumptionKilowattHours);

        return total;
    }

    public async Task<Dictionary<string, decimal>> GetConsumptionByDeviceAsync(DateTime startDate, DateTime endDate)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var result = await context.DeviceEnergyConsumptions
            .Where(e => e.MeasurementTimestamp >= startDate && e.MeasurementTimestamp <= endDate)
            .GroupBy(e => e.SmartHomeDevice.DeviceFriendlyName)
            .Select(g => new { DeviceName = g.Key, Total = g.Sum(e => e.PowerConsumptionKilowattHours) })
            .ToDictionaryAsync(x => x.DeviceName, x => x.Total);

        return result;
    }
}
