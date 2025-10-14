using System.Threading;
using System.Threading.Tasks;

namespace NexusHome.IoT.Core.Services.Interfaces;

/// <summary>
/// Provides comprehensive energy consumption analysis and optimization recommendations
/// Supports real-time monitoring, historical analysis, cost calculations, and predictive analytics
/// </summary>
public interface IEnergyConsumptionAnalyzer
{
    /// <summary>
    /// Calculates real-time energy consumption across all monitored devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current total power consumption and per-device breakdown</returns>
    Task<RealTimeEnergySnapshot> GetRealTimeEnergyConsumptionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes historical energy consumption patterns for specified time period
    /// </summary>
    /// <param name="startDateAnalysis">Analysis period start date</param>
    /// <param name="endDateAnalysis">Analysis period end date</param>
    /// <param name="aggregationInterval">Data aggregation interval (hourly, daily, weekly)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical consumption trends and statistical analysis</returns>
    Task<HistoricalEnergyAnalysis> AnalyzeHistoricalConsumptionAsync(DateTime startDateAnalysis, DateTime endDateAnalysis,
                                                                   EnergyAggregationInterval aggregationInterval,
                                                                   CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates energy costs based on consumption data and utility rate schedules
    /// </summary>
    /// <param name="consumptionStartDate">Cost calculation period start</param>
    /// <param name="consumptionEndDate">Cost calculation period end</param>
    /// <param name="utilityRateSchedule">Utility pricing structure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed cost breakdown including peak/off-peak charges</returns>
    Task<EnergyCostAnalysis> CalculateEnergyCostsAsync(DateTime consumptionStartDate, DateTime consumptionEndDate,
                                                      UtilityRateSchedule utilityRateSchedule,
                                                      CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies devices with anomalous energy consumption patterns
    /// </summary>
    /// <param name="detectionTimeWindow">Time window for anomaly detection</param>
    /// <param name="sensitivityThreshold">Sensitivity level for anomaly detection (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of devices with detected consumption anomalies</returns>
    Task<IEnumerable<EnergyConsumptionAnomaly>> DetectConsumptionAnomaliesAsync(TimeSpan detectionTimeWindow,
                                                                              double sensitivityThreshold = 0.8,
                                                                              CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates energy optimization recommendations based on consumption patterns
    /// </summary>
    /// <param name="optimizationGoal">Primary optimization objective (cost, consumption, carbon footprint)</param>
    /// <param name="analysisDepthDays">Number of days of historical data to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prioritized list of energy optimization recommendations</returns>
    Task<IEnumerable<EnergyOptimizationRecommendation>> GenerateOptimizationRecommendationsAsync(
        OptimizationGoal optimizationGoal, int analysisDepthDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts future energy consumption based on historical patterns and external factors
    /// </summary>
    /// <param name="predictionStartDate">Prediction period start date</param>
    /// <param name="predictionEndDate">Prediction period end date</param>
    /// <param name="includeBehavioralFactors">Whether to include user behavior patterns in prediction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Forecasted energy consumption with confidence intervals</returns>
    Task<EnergyConsumptionForecast> PredictFutureConsumptionAsync(DateTime predictionStartDate, DateTime predictionEndDate,
                                                                bool includeBehavioralFactors = true,
                                                                CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates comprehensive energy usage report with analytics and insights
    /// </summary>
    /// <param name="reportStartDate">Report period start date</param>
    /// <param name="reportEndDate">Report period end date</param>
    /// <param name="includeDeviceBreakdown">Whether to include per-device consumption details</param>
    /// <param name="includeCostAnalysis">Whether to include cost analysis</param>
    /// <param name="includeRecommendations">Whether to include optimization recommendations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive energy usage report</returns>
    Task<ComprehensiveEnergyReport> GenerateEnergyUsageReportAsync(DateTime reportStartDate, DateTime reportEndDate,
                                                                  bool includeDeviceBreakdown = true,
                                                                  bool includeCostAnalysis = true,
                                                                  bool includeRecommendations = true,
                                                                  CancellationToken cancellationToken = default);
}

/// <summary>
/// Energy aggregation intervals for historical analysis
/// </summary>
public enum EnergyAggregationInterval
{
    Hourly,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Optimization goals for energy analysis recommendations
/// </summary>
public enum OptimizationGoal
{
    MinimizeCost,
    ReduceConsumption,
    ReduceCarbonFootprint,
    BalancedOptimization
}

/// <summary>
/// Represents real-time energy consumption snapshot
/// </summary>
public class RealTimeEnergySnapshot
{
    public decimal TotalCurrentPowerWatts { get; set; }
    public DateTime SnapshotTimestamp { get; set; }
    public List<DevicePowerConsumption> DeviceConsumptionBreakdown { get; set; } = new();
    public decimal EstimatedHourlyCost { get; set; }
}

public class DevicePowerConsumption
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public decimal CurrentPowerWatts { get; set; }
    public double PercentageOfTotal { get; set; }
    public string DeviceCategory { get; set; } = string.Empty;
}

public class HistoricalEnergyAnalysis
{
    public DateTime AnalysisStartDate { get; set; }
    public DateTime AnalysisEndDate { get; set; }
    public decimal TotalEnergyConsumedKwh { get; set; }
    public decimal AverageDailyConsumptionKwh { get; set; }
    public decimal PeakPowerConsumptionWatts { get; set; }
    public List<TimeSeriesEnergyData> TimeSeriesData { get; set; } = new();
}

public class TimeSeriesEnergyData
{
    public DateTime Timestamp { get; set; }
    public decimal EnergyConsumedKwh { get; set; }
    public decimal AveragePowerWatts { get; set; }
}

public class EnergyCostAnalysis
{
    public decimal TotalEnergyCost { get; set; }
    public decimal AverageCostPerKwh { get; set; }
    public decimal ProjectedMonthlyCost { get; set; }
    public Dictionary<string, decimal> CostByRatePeriod { get; set; } = new();
}

public class EnergyConsumptionAnomaly
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public DateTime DetectionTimestamp { get; set; }
    public decimal ExpectedConsumptionWatts { get; set; }
    public decimal ActualConsumptionWatts { get; set; }
    public double DeviationPercentage { get; set; }
    public string AnomalyDescription { get; set; } = string.Empty;
}

public class EnergyOptimizationRecommendation
{
    public string RecommendationTitle { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public decimal EstimatedSavingsPerMonth { get; set; }
    public double ImplementationDifficulty { get; set; }
    public int PriorityRanking { get; set; }
}

public class EnergyConsumptionForecast
{
    public List<ForecastDataPoint> ForecastData { get; set; } = new();
    public double ConfidenceLevel { get; set; }
    public string ForecastingMethod { get; set; } = string.Empty;
}

public class ForecastDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal PredictedConsumptionKwh { get; set; }
    public decimal LowerConfidenceBound { get; set; }
    public decimal UpperConfidenceBound { get; set; }
}

public class ComprehensiveEnergyReport
{
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    public RealTimeEnergySnapshot CurrentSnapshot { get; set; } = new();
    public HistoricalEnergyAnalysis HistoricalAnalysis { get; set; } = new();
    public EnergyCostAnalysis CostAnalysis { get; set; } = new();
    public List<EnergyOptimizationRecommendation> Recommendations { get; set; } = new();
    public DateTime ReportGeneratedAt { get; set; }
}

public class UtilityRateSchedule
{
    public string RatePlanName { get; set; } = string.Empty;
    public Dictionary<string, decimal> RatesByPeriod { get; set; } = new();
    public TimeSpan PeakHoursStart { get; set; }
    public TimeSpan PeakHoursEnd { get; set; }
    public decimal BaseServiceCharge { get; set; }
}
