using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Core.DTOs;

namespace NexusHome.IoT.Core.Services;

public class EnergyOptimizationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnergyOptimizationBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public EnergyOptimizationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EnergyOptimizationBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int>("EnergyManagement:OptimizationIntervalMinutes", 15);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation("Energy Optimization Service started with interval: {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var optimizationService = scope.ServiceProvider.GetRequiredService<IEnergyOptimizationService>();

                // Run optimization every interval
                var startTime = DateTime.UtcNow;
                var endTime = startTime.AddHours(24);

                var result = await optimizationService.OptimizeEnergyUsageAsync(startTime, endTime);

                _logger.LogInformation("Energy optimization completed. Potential savings: {Savings:C}", 
                    result.EstimatedCostSavings);

                // Execute high-priority optimization strategies automatically
                var autoExecuteStrategies = result.Strategies
                    .Where(s => s.AutoExecute && s.ComfortImpact < 2.0)
                    .Take(3)
                    .ToList();

                foreach (var strategy in autoExecuteStrategies)
                {
                    try
                    {
                        var plan = ConvertStrategyToPlan(strategy);
                        await optimizationService.ExecuteOptimizationPlanAsync(plan);
                        _logger.LogInformation("Auto-executed optimization strategy: {StrategyName}", strategy.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-executing optimization strategy: {StrategyName}", strategy.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in energy optimization background service");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private OptimizationPlan ConvertStrategyToPlan(OptimizationStrategy strategy)
    {
        return new OptimizationPlan
        {
            Name = strategy.Name,
            Actions = new List<OptimizationAction>
            {
                new OptimizationAction
                {
                    ActionId = Guid.NewGuid().ToString(),
                    ActionType = OptimizationActionType.DeviceControl, // Defaulting
                    DeviceId = strategy.TargetDeviceId,
                    Parameters = strategy.Parameters,
                    ExecutionOrder = 1,
                    ExecutionStatus = OptimizationActionStatus.Pending
                }
            },
            ExecutionStatus = OptimizationPlanStatus.Pending,
            ExecutedAt = null
        };
    }
}
