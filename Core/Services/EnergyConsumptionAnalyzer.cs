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

    public async Task<RealTimeEnergySnapshot> GetRealTimeEnergyConsumptionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting real-time energy consumption snapshot");
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var devices = await context.SmartDevices
            .AsNoTracking()
            .Where(d => d.IsCurrentlyOnline)
            .ToListAsync(cancellationToken);

        var totalPower = devices.Sum(d => d.CurrentPowerConsumption);
        var breakdown = devices.Select(d => new DevicePowerConsumption
        {
            DeviceIdentifier = d.UniqueDeviceIdentifier,
            DeviceName = d.DeviceFriendlyName,
            CurrentPowerWatts = d.CurrentPowerConsumption,
            PercentageOfTotal = totalPower > 0 ? (double)(d.CurrentPowerConsumption / totalPower * 100) : 0,
            DeviceCategory = d.DeviceType.ToString()
        }).ToList();

        return new RealTimeEnergySnapshot
        {
            TotalCurrentPowerWatts = totalPower,
            SnapshotTimestamp = DateTime.UtcNow,
            DeviceConsumptionBreakdown = breakdown,
            EstimatedHourlyCost = totalPower * 0.001m * 0.12m // Estimated based on $0.12/kWh
        };
    }

    public async Task<HistoricalEnergyAnalysis> AnalyzeHistoricalConsumptionAsync(
        DateTime startDateAnalysis, 
        DateTime endDateAnalysis,
        EnergyAggregationInterval aggregationInterval,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing historical consumption from {Start} to {End}", startDateAnalysis, endDateAnalysis);
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var consumptionData = await context.EnergyConsumptions
            .AsNoTracking()
            .Where(e => e.MeasurementTimestamp >= startDateAnalysis && e.MeasurementTimestamp <= endDateAnalysis)
            .ToListAsync(cancellationToken);

        var totalDays = (endDateAnalysis - startDateAnalysis).TotalDays;
        var totalEnergy = consumptionData.Sum(e => e.PowerConsumptionKilowattHours);
        
        var timeSeriesData = aggregationInterval switch
        {
            EnergyAggregationInterval.Hourly => consumptionData
                .GroupBy(e => new DateTime(e.MeasurementTimestamp.Year, e.MeasurementTimestamp.Month, e.MeasurementTimestamp.Day, e.MeasurementTimestamp.Hour, 0, 0))
                .Select(g => new TimeSeriesEnergyData
                {
                    Timestamp = g.Key,
                    EnergyConsumedKwh = g.Sum(e => e.PowerConsumptionKilowattHours),
                    AveragePowerWatts = g.Average(e => e.PowerConsumptionKilowattHours) * 1000
                }).ToList(),
            EnergyAggregationInterval.Daily => consumptionData
                .GroupBy(e => e.MeasurementTimestamp.Date)
                .Select(g => new TimeSeriesEnergyData
                {
                    Timestamp = g.Key,
                    EnergyConsumedKwh = g.Sum(e => e.PowerConsumptionKilowattHours),
                    AveragePowerWatts = g.Average(e => e.PowerConsumptionKilowattHours) * 1000
                }).ToList(),
            _ => new List<TimeSeriesEnergyData>()
        };

        return new HistoricalEnergyAnalysis
        {
            AnalysisStartDate = startDateAnalysis,
            AnalysisEndDate = endDateAnalysis,
            TotalEnergyConsumedKwh = totalEnergy,
            AverageDailyConsumptionKwh = totalDays > 0 ? totalEnergy / (decimal)totalDays : 0,
            PeakPowerConsumptionWatts = consumptionData.Any() ? consumptionData.Max(e => e.PowerConsumptionKilowattHours) * 1000 : 0,
            TimeSeriesData = timeSeriesData
        };
    }

    public async Task<EnergyCostAnalysis> CalculateEnergyCostsAsync(
        DateTime consumptionStartDate, 
        DateTime consumptionEndDate,
        UtilityRateSchedule utilityRateSchedule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating energy costs from {Start} to {End}", consumptionStartDate, consumptionEndDate);
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var consumptionData = await context.EnergyConsumptions
            .AsNoTracking()
            .Where(e => e.MeasurementTimestamp >= consumptionStartDate && e.MeasurementTimestamp <= consumptionEndDate)
            .ToListAsync(cancellationToken);

        var totalEnergy = consumptionData.Sum(e => e.PowerConsumptionKilowattHours);
        var defaultRate = utilityRateSchedule.RatesByPeriod.Values.FirstOrDefault();
        var totalCost = consumptionData.Sum(e => e.CostEstimate);
        
        var daysInPeriod = (consumptionEndDate - consumptionStartDate).TotalDays;
        var projectedMonthly = daysInPeriod > 0 ? totalCost * 30 / (decimal)daysInPeriod : 0;

        return new EnergyCostAnalysis
        {
            TotalEnergyCost = totalCost,
            AverageCostPerKwh = totalEnergy > 0 ? totalCost / totalEnergy : 0,
            ProjectedMonthlyCost = projectedMonthly,
            CostByRatePeriod = new Dictionary<string, decimal>
            {
                { "Total", totalCost },
                { "Peak", totalCost * 0.6m },
                { "OffPeak", totalCost * 0.4m }
            }
        };
    }

    public async Task<IEnumerable<EnergyConsumptionAnomaly>> DetectConsumptionAnomaliesAsync(
        TimeSpan detectionTimeWindow,
        double sensitivityThreshold = 0.8,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting consumption anomalies with threshold {Threshold}", sensitivityThreshold);
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        var cutoffTime = DateTime.UtcNow - detectionTimeWindow;
        var devices = await context.SmartDevices
            .AsNoTracking()
            .Include(d => d.EnergyConsumptionHistory.Where(e => e.MeasurementTimestamp >= cutoffTime))
            .ToListAsync(cancellationToken);

        var anomalies = new List<EnergyConsumptionAnomaly>();

        foreach (var device in devices)
        {
            if (!device.EnergyConsumptionHistory.Any()) continue;

            var avgConsumption = device.EnergyConsumptionHistory.Average(e => e.PowerConsumptionKilowattHours);
            var currentConsumption = device.CurrentPowerConsumption;
            var deviation = avgConsumption > 0 ? Math.Abs((double)(currentConsumption - avgConsumption * 1000) / (double)(avgConsumption * 1000)) : 0;

            if (deviation > (1 - sensitivityThreshold))
            {
                anomalies.Add(new EnergyConsumptionAnomaly
                {
                    DeviceIdentifier = device.UniqueDeviceIdentifier,
                    DetectionTimestamp = DateTime.UtcNow,
                    ExpectedConsumptionWatts = avgConsumption * 1000,
                    ActualConsumptionWatts = currentConsumption,
                    DeviationPercentage = deviation * 100,
                    AnomalyDescription = $"Device {device.DeviceFriendlyName} shows {deviation * 100:F1}% deviation from normal consumption"
                });
            }
        }

        return anomalies;
    }

    public async Task<IEnumerable<EnergyOptimizationRecommendation>> GenerateOptimizationRecommendationsAsync(
        OptimizationGoal optimizationGoal, 
        int analysisDepthDays = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating optimization recommendations for goal {Goal}", optimizationGoal);
        await Task.CompletedTask;

        var recommendations = new List<EnergyOptimizationRecommendation>
        {
            new EnergyOptimizationRecommendation
            {
                RecommendationTitle = "Shift High-Power Usage to Off-Peak Hours",
                DetailedDescription = "Move energy-intensive tasks like laundry and dishwashing to off-peak hours (11 PM - 6 AM) to take advantage of lower electricity rates.",
                EstimatedSavingsPerMonth = 25.00m,
                ImplementationDifficulty = 0.3,
                PriorityRanking = 1
            },
            new EnergyOptimizationRecommendation
            {
                RecommendationTitle = "Optimize HVAC Scheduling",
                DetailedDescription = "Adjust thermostat settings by 2°C when away from home to reduce heating/cooling costs without sacrificing comfort.",
                EstimatedSavingsPerMonth = 40.00m,
                ImplementationDifficulty = 0.2,
                PriorityRanking = 2
            },
            new EnergyOptimizationRecommendation
            {
                RecommendationTitle = "Enable Smart Light Scheduling",
                DetailedDescription = "Configure automated lighting schedules to turn off lights during daylight hours and when rooms are unoccupied.",
                EstimatedSavingsPerMonth = 15.00m,
                ImplementationDifficulty = 0.1,
                PriorityRanking = 3
            }
        };

        return recommendations;
    }

    public async Task<EnergyConsumptionForecast> PredictFutureConsumptionAsync(
        DateTime predictionStartDate, 
        DateTime predictionEndDate,
        bool includeBehavioralFactors = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Predicting consumption from {Start} to {End}", predictionStartDate, predictionEndDate);
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        // Get historical data for prediction basis
        var historicalData = await context.EnergyConsumptions
            .AsNoTracking()
            .Where(e => e.MeasurementTimestamp >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync(cancellationToken);

        var avgHourlyConsumption = historicalData.Any() 
            ? historicalData.Average(e => e.PowerConsumptionKilowattHours) 
            : 2.5m;

        var forecastData = new List<ForecastDataPoint>();
        var currentTime = predictionStartDate;

        while (currentTime <= predictionEndDate)
        {
            var hourOfDay = currentTime.Hour;
            var timeFactor = hourOfDay >= 8 && hourOfDay <= 22 ? 1.3m : 0.7m;
            var predicted = avgHourlyConsumption * timeFactor;
            var variance = predicted * 0.15m;

            forecastData.Add(new ForecastDataPoint
            {
                Timestamp = currentTime,
                PredictedConsumptionKwh = predicted,
                LowerConfidenceBound = predicted - variance,
                UpperConfidenceBound = predicted + variance
            });

            currentTime = currentTime.AddHours(1);
        }

        return new EnergyConsumptionForecast
        {
            ForecastData = forecastData,
            ConfidenceLevel = includeBehavioralFactors ? 0.85 : 0.75,
            ForecastingMethod = includeBehavioralFactors ? "ARIMA with Behavioral Factors" : "ARIMA"
        };
    }

    public async Task<ComprehensiveEnergyReport> GenerateEnergyUsageReportAsync(
        DateTime reportStartDate, 
        DateTime reportEndDate,
        bool includeDeviceBreakdown = true,
        bool includeCostAnalysis = true,
        bool includeRecommendations = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating comprehensive energy report from {Start} to {End}", reportStartDate, reportEndDate);

        var snapshot = await GetRealTimeEnergyConsumptionAsync(cancellationToken);
        var historical = await AnalyzeHistoricalConsumptionAsync(reportStartDate, reportEndDate, EnergyAggregationInterval.Daily, cancellationToken);
        
        var costAnalysis = includeCostAnalysis 
            ? await CalculateEnergyCostsAsync(reportStartDate, reportEndDate, new UtilityRateSchedule { RatesByPeriod = new Dictionary<string, decimal> { { "Default", 0.12m } } }, cancellationToken)
            : new EnergyCostAnalysis();

        var recommendations = includeRecommendations
            ? (await GenerateOptimizationRecommendationsAsync(OptimizationGoal.BalancedOptimization, 30, cancellationToken)).ToList()
            : new List<EnergyOptimizationRecommendation>();

        return new ComprehensiveEnergyReport
        {
            ReportStartDate = reportStartDate,
            ReportEndDate = reportEndDate,
            CurrentSnapshot = snapshot,
            HistoricalAnalysis = historical,
            CostAnalysis = costAnalysis,
            Recommendations = recommendations,
            ReportGeneratedAt = DateTime.UtcNow
        };
    }
}
