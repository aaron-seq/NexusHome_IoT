using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Core.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace NexusHome.IoT.Core.Services;

public class EnergyOptimizationService : IEnergyOptimizationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnergyOptimizationService> _logger;
    private readonly IMqttClientService _mqttService; // Changed to IMqttClientService
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, decimal> _energyRates;
    private readonly TimeSpan _peakHoursStart;
    private readonly TimeSpan _peakHoursEnd;
    private readonly TimeSpan _offPeakStart;
    private readonly TimeSpan _offPeakEnd;

    public EnergyOptimizationService(
        IServiceProvider serviceProvider,
        ILogger<EnergyOptimizationService> logger,
        IMqttClientService mqttService,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mqttService = mqttService;
        _configuration = configuration;

        // Load energy rates and time periods from configuration
        // Using safe defaults if config is missing
        _energyRates = new Dictionary<string, decimal>
        {
            ["peak"] = _configuration.GetValue<decimal>("EnergyManagement:EnergyRates:PeakRate", 0.30m),
            ["standard"] = _configuration.GetValue<decimal>("EnergyManagement:EnergyRates:StandardRate", 0.15m),
            ["offpeak"] = _configuration.GetValue<decimal>("EnergyManagement:EnergyRates:OffPeakRate", 0.08m),
            ["solar"] = _configuration.GetValue<decimal>("EnergyManagement:EnergyRates:SolarFeedInRate", 0.05m)
        };

        TimeSpan.TryParse(_configuration["EnergyManagement:PeakHours:Start"] ?? "17:00", out _peakHoursStart);
        TimeSpan.TryParse(_configuration["EnergyManagement:PeakHours:End"] ?? "21:00", out _peakHoursEnd);
        TimeSpan.TryParse(_configuration["EnergyManagement:OffPeakHours:Start"] ?? "23:00", out _offPeakStart);
        TimeSpan.TryParse(_configuration["EnergyManagement:OffPeakHours:End"] ?? "06:00", out _offPeakEnd);
    }

    /* ... Existing logic ported ... */

    public async Task<OptimizationResult> OptimizeEnergyUsageAsync(DateTime startTime, DateTime endTime)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>(); // Changed Context

        try
        {
            _logger.LogInformation("Starting energy optimization for period {StartTime} to {EndTime}", startTime, endTime);

            var currentConsumption = await GetCurrentEnergyConsumptionAsync(context);
            var weatherForecast = await GetWeatherForecastAsync(context, startTime, endTime);
            var batteryStatus = await GetCurrentBatteryStatusAsync(context);
            var solarForecast = await ForecastSolarGenerationAsync(context, weatherForecast);

            var strategies = new List<OptimizationStrategy>
            {
                await GenerateLoadShiftingStrategyAsync(context, startTime, endTime),
                await GeneratePeakShavingStrategyAsync(context, currentConsumption),
                await GenerateBatteryOptimizationStrategyAsync(context, batteryStatus, solarForecast),
                await GenerateThermalOptimizationStrategyAsync(context, weatherForecast),
                await GenerateApplianceSchedulingStrategyAsync(context, startTime, endTime)
            };

            var optimizedStrategies = new List<OptimizationStrategy>();
            foreach (var strategy in strategies.Where(s => s != null))
            {
                strategy.PotentialSavings = await CalculatePotentialSavingsAsync(context, strategy);
                strategy.ComfortImpact = CalculateComfortImpact(strategy);
                strategy.ImplementationComplexity = CalculateImplementationComplexity(strategy);
                
                if (strategy.PotentialSavings > 0)
                {
                    optimizedStrategies.Add(strategy);
                }
            }

            var rankedStrategies = optimizedStrategies
                .OrderByDescending(s => s.PotentialSavings / (decimal)(1 + s.ComfortImpact))
                .ToList();

            var result = new OptimizationResult
            {
                OptimizationTimestamp = DateTime.UtcNow,
                OptimizationPeriod = new DateRange { Start = startTime, End = endTime },
                CurrentConsumption = currentConsumption,
                Strategies = rankedStrategies,
                TotalPotentialSavings = rankedStrategies.Sum(s => s.PotentialSavings),
                EstimatedCostSavings = rankedStrategies.Sum(s => s.PotentialSavings * GetCurrentEnergyRate()),
                ComfortScore = CalculateOverallComfortScore(rankedStrategies),
                EnvironmentalImpact = CalculateEnvironmentalImpact(rankedStrategies),
                RecommendedActions = GenerateActionRecommendations(rankedStrategies),
                ImplementationPriority = rankedStrategies.Take(3).ToList()
            };

            _logger.LogInformation("Energy optimization completed. Potential savings: {Savings:C}", result.EstimatedCostSavings);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during energy optimization");
            throw;
        }
    }

    public async Task<LoadShiftingRecommendation> GenerateLoadShiftingRecommendationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        try
        {
            // Porting DeviceType mapping
            var deferrableDevices = await context.SmartDevices
                .Where(d => d.DeviceType == DeviceCategory.LaundryWashingMachine ||
                           d.DeviceType == DeviceCategory.ClothesDryingMachine ||
                           d.DeviceType == DeviceCategory.DishwashingAppliance ||
                           d.DeviceType == DeviceCategory.ElectricVehicleCharger)
                .Where(d => d.CurrentStatus == DeviceOperationalStatus.ActiveAndRunning)
                .ToListAsync();

            var recommendations = new List<LoadShiftingAction>();

            foreach (var device in deferrableDevices)
            {
                var currentSchedule = await GetDeviceCurrentScheduleAsync(context, device.Id);
                var optimalSchedule = CalculateOptimalSchedule(device, currentSchedule);

                if (optimalSchedule != null) // Check not null
                {
                    var potentialSavings = await CalculateLoadShiftingSavingsAsync(context, device, currentSchedule, optimalSchedule);

                    recommendations.Add(new LoadShiftingAction
                    {
                        DeviceId = device.Id,
                        DeviceName = device.DeviceFriendlyName,
                        CurrentSchedule = currentSchedule,
                        RecommendedSchedule = optimalSchedule,
                        PotentialSavings = potentialSavings,
                        ShiftReason = DetermineShiftReason(currentSchedule, optimalSchedule),
                        Priority = potentialSavings > 5 ? LoadShiftingPriority.High : LoadShiftingPriority.Medium
                    });
                }
            }

            return new LoadShiftingRecommendation
            {
                GeneratedAt = DateTime.UtcNow,
                Actions = recommendations.OrderByDescending(r => r.PotentialSavings).ToList(),
                TotalPotentialSavings = recommendations.Sum(r => r.PotentialSavings),
                OptimizationHorizon = TimeSpan.FromHours(24),
                Confidence = CalculateLoadShiftingConfidence(recommendations)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating load shifting recommendations");
            throw;
        }
    }

    // ... Stub implementations for missing methods ...

    private async Task<OptimizationStrategy> GenerateLoadShiftingStrategyAsync(SmartHomeDbContext context, DateTime start, DateTime end)
    {
        // Stub
        return await Task.FromResult(new OptimizationStrategy { Name = "Load Shifting", Type = OptimizationRuleType.LoadShifting, PotentialSavings = 1.5m });
    }

    private async Task<OptimizationStrategy> GeneratePeakShavingStrategyAsync(SmartHomeDbContext context, decimal currentConsumption)
    {
        return await Task.FromResult(new OptimizationStrategy { Name = "Peak Shaving", Type = OptimizationRuleType.PeakShaving });
    }

    private async Task<OptimizationStrategy> GenerateBatteryOptimizationStrategyAsync(SmartHomeDbContext context, BatteryStatus? battery, List<SolarGenerationForecast> solar)
    {
        return await Task.FromResult(new OptimizationStrategy { Name = "Battery Opt", Type = OptimizationRuleType.BatteryOptimization });
    }

    private async Task<OptimizationStrategy> GenerateThermalOptimizationStrategyAsync(SmartHomeDbContext context, List<WeatherData> weather)
    {
        return await Task.FromResult(new OptimizationStrategy { Name = "Thermal Opt", Type = OptimizationRuleType.ThermalStorage });
    }
    
    private async Task<OptimizationStrategy> GenerateApplianceSchedulingStrategyAsync(SmartHomeDbContext context, DateTime start, DateTime end)
    {
        return await Task.FromResult(new OptimizationStrategy { Name = "Appliance Scheduling", Type = OptimizationRuleType.SmartApplianceScheduling });
    }

    private Task<decimal> CalculatePotentialSavingsAsync(SmartHomeDbContext context, OptimizationStrategy strategy)
    {
        return Task.FromResult(strategy.PotentialSavings);
    }

    private double CalculateComfortImpact(OptimizationStrategy strategy) => 0.0;
    private double CalculateImplementationComplexity(OptimizationStrategy strategy) => 0.0;
    
    private decimal CalculateOverallComfortScore(List<OptimizationStrategy> strategies) => 1.0m;
    private decimal CalculateEnvironmentalImpact(List<OptimizationStrategy> strategies) => 0.0m;
    
    private List<string> GenerateActionRecommendations(List<OptimizationStrategy> strategies) => new List<string> { "Action 1" };

    private Task<object> GetDeviceCurrentScheduleAsync(SmartHomeDbContext context, int deviceId) => Task.FromResult((object)new { });
    private object CalculateOptimalSchedule(SmartHomeDevice device, object current) => new { };
    private Task<decimal> CalculateLoadShiftingSavingsAsync(SmartHomeDbContext context, SmartHomeDevice device, object current, object optimal) => Task.FromResult(0.0m);
    private string DetermineShiftReason(object current, object optimal) => "Cost";
    private double CalculateLoadShiftingConfidence(List<LoadShiftingAction> actions) => 0.8;

    private async Task<decimal> GetCurrentEnergyConsumptionAsync(SmartHomeDbContext context)
    {
         return await context.EnergyConsumptions
                .Where(e => e.MeasurementTimestamp >= DateTime.UtcNow.AddMinutes(-15))
                .SumAsync(e => e.PowerConsumptionKilowattHours);
    }

    private async Task<List<WeatherData>> GetWeatherForecastAsync(SmartHomeDbContext context, DateTime start, DateTime end)
    {
        return await context.WeatherData
            .Where(w => w.Timestamp >= start && w.Timestamp <= end)
            .ToListAsync();
    }

    private async Task<BatteryStatus?> GetCurrentBatteryStatusAsync(SmartHomeDbContext context)
    {
        return await context.BatteryStatuses.OrderByDescending(b => b.MeasurementTimestamp).FirstOrDefaultAsync();
    }

    private async Task<List<SolarGenerationForecast>> ForecastSolarGenerationAsync(SmartHomeDbContext context, List<WeatherData> weather)
    {
         // Simple stub logic or ported logic from original
         return weather.Select(w => new SolarGenerationForecast { Timestamp = w.Timestamp, PredictedGeneration = 5.0m, Confidence = 0.9 }).ToList();
    }

    private decimal GetCurrentEnergyRate() => 0.15m;

    // Remaining public methods stubs
    public Task<BatteryOptimizationPlan> OptimizeBatteryUsageAsync() => Task.FromResult(new BatteryOptimizationPlan());
    public Task<SolarOptimizationPlan> OptimizeSolarEnergyUsageAsync() => Task.FromResult(new SolarOptimizationPlan());
    public Task<CostOptimizationResult> OptimizeEnergyCostsAsync() => Task.FromResult(new CostOptimizationResult());
    public Task<DemandResponseResult> HandleDemandResponseEventAsync(DemandResponseEvent demandEvent) => Task.FromResult(new DemandResponseResult());
    public Task<EnergyForecast> ForecastEnergyDemandAsync(DateTime startDate, int forecastDays) => Task.FromResult(new EnergyForecast());
    public Task ExecuteOptimizationPlanAsync(OptimizationPlan plan) => Task.CompletedTask;
    public Task<ComfortOptimizationResult> OptimizeComfortVsEfficiencyAsync(ComfortPreferences preferences) => Task.FromResult(new ComfortOptimizationResult());

}
