using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Core.DTOs;

public class MaintenancePredictionResult
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public double FailureProbability { get; set; }
    public double Confidence { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class HealthScoreResult
{
    public int DeviceId { get; set; }
    public int HealthScore { get; set; }
}

public class MaintenancePrediction
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DeviceCategory DeviceType { get; set; }
    public double FailureProbability { get; set; }
    public DateTime? PredictedFailureDate { get; set; }
    public double Confidence { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public Dictionary<string, float> FeatureImportance { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class MaintenanceFeatures
{
    public float AveragePowerConsumption { get; set; }
    public float PowerConsumptionStdDev { get; set; }
    public float AverageVoltage { get; set; }
    public float AverageCurrent { get; set; }
    public float AverageTemperature { get; set; }
    public float AverageVibration { get; set; }
    public float OperatingHours { get; set; }
    public float PowerTrend { get; set; }
    public float TemperatureTrend { get; set; }
    public float DaysSinceLastMaintenance { get; set; }
    public float AnomalyScore { get; set; }
}

public class AnomalyDetectionResult
{
    public int DeviceId { get; set; }
    public bool HasAnomalies { get; set; }
    public int AnomalyCount { get; set; }
    public List<AnomalyPoint> Anomalies { get; set; } = new();
    public double Confidence { get; set; }
    public DateTime DetectionTimestamp { get; set; }
}

public class AnomalyPoint
{
    public int Index { get; set; }
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public double Score { get; set; }
}

public class TimeSeriesData
{
    public DateTime Timestamp { get; set; }
    public float Value { get; set; }
}

public class EnergyConsumptionPrediction
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime PredictionDate { get; set; }
    public decimal PredictedConsumption { get; set; }
    public double Confidence { get; set; }
    public Range<decimal> PredictionRange { get; set; } = new Range<decimal>(0, 0);
    public DateTime PredictionTimestamp { get; set; }
}

// EnergyForecast definition removed

public class MaintenanceFeedback
{
    public string FeedbackType { get; set; } = string.Empty;
    public double PredictedFailureProbability { get; set; }
    public bool ActualOutcome { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class DeviceHistoricalData
{
    public DateTime Timestamp { get; set; }
    public decimal PowerConsumption { get; set; }
    public decimal Voltage { get; set; }
    public decimal Current { get; set; }
    public decimal PowerFactor { get; set; }
    public float Temperature { get; set; }
    public float Vibration { get; set; }
    public float OperatingHours { get; set; }
    public bool MaintenanceFlag { get; set; }
}

// EnergyForecast removed to avoid conflict with EnergyOptimizationDTOs
// Range<T> moved to CommonDTOs.cs
