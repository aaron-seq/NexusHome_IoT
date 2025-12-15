using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Core.DTOs;

public class OptimizationResult
{
    public DateTime OptimizationTimestamp { get; set; }
    public DateRange OptimizationPeriod { get; set; } = new();
    public decimal CurrentConsumption { get; set; }
    public List<OptimizationStrategy> Strategies { get; set; } = new();
    public decimal TotalPotentialSavings { get; set; }
    public decimal EstimatedCostSavings { get; set; }
    public decimal ComfortScore { get; set; }
    public decimal EnvironmentalImpact { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public List<OptimizationStrategy> ImplementationPriority { get; set; } = new();
}

public class OptimizationStrategy
{
    public string Name { get; set; } = string.Empty;
    public OptimizationRuleType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
    public double ComfortImpact { get; set; } // 0-10 scale
    public double ImplementationComplexity { get; set; } // 0-10 scale
    public bool AutoExecute { get; set; }
    public int? TargetDeviceId { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class LoadShiftingRecommendation
{
    public DateTime GeneratedAt { get; set; }
    public List<LoadShiftingAction> Actions { get; set; } = new();
    public decimal TotalPotentialSavings { get; set; }
    public TimeSpan OptimizationHorizon { get; set; }
    public double Confidence { get; set; }
}

public class LoadShiftingAction
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public object CurrentSchedule { get; set; }
    public object RecommendedSchedule { get; set; }
    public decimal PotentialSavings { get; set; }
    public string ShiftReason { get; set; } = string.Empty;
    public LoadShiftingPriority Priority { get; set; }
}

public enum LoadShiftingPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class BatteryOptimizationPlan
{
    public DateTime GeneratedAt { get; set; }
    public List<BatteryOptimizationAction> Actions { get; set; } = new();
    public decimal TotalExpectedSavings { get; set; }
    public TimeSpan OptimizationHorizon { get; set; }
    public double Confidence { get; set; }
}

public class BatteryOptimizationAction
{
    public int BatteryId { get; set; }
    public string BatteryName { get; set; } = string.Empty;
    public decimal CurrentChargeLevel { get; set; }
    public object ChargingSchedule { get; set; }
    public object DischargingSchedule { get; set; }
    public decimal ExpectedSavings { get; set; }
    public string OptimizationReason { get; set; } = string.Empty;
}

public class SolarOptimizationPlan
{
    public DateTime GeneratedAt { get; set; }
    public List<SolarOptimizationAction> Actions { get; set; } = new();
    public decimal TotalOptimizationValue { get; set; }
    public decimal SelfConsumptionRatio { get; set; }
    public TimeSpan OptimizationHorizon { get; set; }
}

public class SolarOptimizationAction
{
    public int SolarDeviceId { get; set; }
    public string SolarDeviceName { get; set; } = string.Empty;
    public decimal ExpectedGeneration { get; set; }
    public object DirectConsumptionPlan { get; set; }
    public object StorageAllocationPlan { get; set; }
    public object GridExportPlan { get; set; }
    public decimal OptimizationValue { get; set; }
}

public class CostOptimizationResult
{
    public DateTime GeneratedAt { get; set; }
    public List<CostOptimizationAction> Actions { get; set; } = new();
    public decimal TotalDailySavings { get; set; }
    public decimal TotalMonthlySavings { get; set; }
    public decimal TotalYearlySavings { get; set; }
    public TimeSpan PaybackPeriod { get; set; }
    public double Confidence { get; set; }
}

public class CostOptimizationAction
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public decimal CurrentDailyCost { get; set; }
    public decimal OptimizedDailyCost { get; set; }
    public decimal PotentialDailySavings { get; set; }
    public List<string> OptimizationMethods { get; set; } = new();
    public double ImplementationDifficulty { get; set; }
}

public class DemandResponseEvent
{
    public string EventId { get; set; } = string.Empty;
    public DemandResponseEventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TargetReduction { get; set; }
}

public enum DemandResponseEventType
{
    PeakShaving,
    LoadReduction,
    FrequencyRegulation,
    EmergencyResponse
}

public class DemandResponseResult
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ResponseTimestamp { get; set; }
    public List<DemandResponseAction> Actions { get; set; } = new();
    public decimal TotalPowerReduction { get; set; }
    public decimal TargetReduction { get; set; }
    public bool ReductionAchieved { get; set; }
    public decimal IncentiveEarnings { get; set; }
    public double ComfortImpact { get; set; }
    public TimeSpan Duration { get; set; }
}

public class DemandResponseAction
{
    public string ActionId { get; set; }
    public decimal PowerReduction { get; set; }
    public DemandResponsePriority Priority { get; set; }
}

public enum DemandResponsePriority
{
    Low,
    Medium,
    High,
    Immediate
}

public class EnergyForecast
{
    public DateTime GeneratedAt { get; set; }
    public DateRange ForecastPeriod { get; set; } = new();
    public List<EnergyForecastPoint> ForecastPoints { get; set; } = new();
    public decimal TotalPredictedConsumption { get; set; }
    public decimal AverageConfidence { get; set; }
    public List<DateRange> PeakDemandPeriods { get; set; } = new();
    public List<DateRange> LowDemandPeriods { get; set; } = new();
}

public class EnergyForecastPoint
{
    public DateTime Timestamp { get; set; }
    public decimal PredictedConsumption { get; set; }
    public double Confidence { get; set; } // 0-1
    public Range<decimal> PredictionInterval { get; set; }
    public decimal WeatherImpact { get; set; }
    public decimal SeasonalFactor { get; set; }
}

// Range<T> moved to CommonDTOs.cs

public class OptimizationPlan
{
    public string Name { get; set; } = string.Empty;
    public List<OptimizationAction> Actions { get; set; } = new();
    public OptimizationPlanStatus ExecutionStatus { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public class OptimizationAction
{
    public string ActionId { get; set; } = string.Empty;
    public OptimizationActionType ActionType { get; set; }
    public int? DeviceId { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int ExecutionOrder { get; set; }
    public OptimizationActionStatus ExecutionStatus { get; set; }
    public DateTime? ExecutionTimestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum OptimizationActionType
{
    DeviceControl,
    ScheduleChange,
    BatteryOperation,
    LoadShifting,
    ThermalAdjustment
}

public enum OptimizationActionStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum OptimizationPlanStatus
{
    Pending,
    InProgress,
    Completed,
    PartiallyCompleted,
    Failed,
    Cancelled
}

public class ComfortPreferences
{
    public decimal MinTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    // Add other preferences as needed
}

public class ComfortOptimizationResult
{
    public DateTime GeneratedAt { get; set; }
    public ComfortPreferences UserPreferences { get; set; }
    public List<ComfortOptimizationAction> Actions { get; set; } = new();
    public decimal TotalEnergySavings { get; set; }
    public decimal AverageComfortImpact { get; set; }
    public decimal ComfortEfficiencyRatio { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public class ComfortOptimizationAction
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DeviceCategory DeviceType { get; set; }
    public object CurrentSettings { get; set; }
    public object OptimalSettings { get; set; }
    public decimal ComfortImpact { get; set; }
    public decimal EfficiencyGain { get; set; }
    public decimal EnergySavings { get; set; }
}

public class SolarGenerationForecast
{
    public DateTime Timestamp { get; set; }
    public decimal PredictedGeneration { get; set; }
    public double Confidence { get; set; }
    public string WeatherConditions { get; set; } = string.Empty;
}
