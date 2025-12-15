using NexusHome.IoT.Core.DTOs;

namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IEnergyOptimizationService
{
    Task<OptimizationResult> OptimizeEnergyUsageAsync(DateTime startTime, DateTime endTime);
    Task<LoadShiftingRecommendation> GenerateLoadShiftingRecommendationsAsync();
    Task<BatteryOptimizationPlan> OptimizeBatteryUsageAsync();
    Task<SolarOptimizationPlan> OptimizeSolarEnergyUsageAsync();
    Task<CostOptimizationResult> OptimizeEnergyCostsAsync();
    Task<DemandResponseResult> HandleDemandResponseEventAsync(DemandResponseEvent demandEvent);
    Task<EnergyForecast> ForecastEnergyDemandAsync(DateTime startDate, int forecastDays);
    Task ExecuteOptimizationPlanAsync(OptimizationPlan plan);
    Task<ComfortOptimizationResult> OptimizeComfortVsEfficiencyAsync(ComfortPreferences preferences);
}
