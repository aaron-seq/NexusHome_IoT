namespace NexusHome.IoT.Application.DTOs;

/// <summary>
/// Summary of energy consumption over a period
/// </summary>
public class EnergyConsumptionSummaryDto
{
    public decimal TotalConsumption { get; set; }
    
    public decimal AverageConsumption { get; set; }
    
    public decimal PeakConsumption { get; set; }
    
    public DateTime PeakTime { get; set; }
    
    public DatePeriod Period { get; set; } = new();
    
    public DateTime Timestamp { get; set; }
    
    public List<DeviceConsumptionBreakdown> DeviceBreakdown { get; set; } = new();
}

/// <summary>
/// Date period for energy queries
/// </summary>
public class DatePeriod
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

/// <summary>
/// Energy consumption breakdown by device
/// </summary>
public class DeviceConsumptionBreakdown
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public decimal Consumption { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Detailed cost analysis for energy consumption
/// </summary>
public class EnergyCostAnalysisDto
{
    public decimal TotalCost { get; set; }
    
    public decimal AverageCostPerKwh { get; set; }
    
    public List<DailyCostBreakdown> DailyCosts { get; set; } = new();
    
    public DatePeriod Period { get; set; } = new();
    
    public List<CostOptimizationSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// Daily cost breakdown
/// </summary>
public class DailyCostBreakdown
{
    public DateTime Date { get; set; }
    public decimal Consumption { get; set; }
    public decimal Cost { get; set; }
}

/// <summary>
/// Cost optimization suggestion
/// </summary>
public class CostOptimizationSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedSavings { get; set; }
}

/// <summary>
/// Energy usage forecast
/// </summary>
public class EnergyForecastDto
{
    public List<ForecastDataPoint> Forecast { get; set; } = new();
    
    public int HoursAhead { get; set; }
    
    public decimal PredictedConsumption { get; set; }
    
    public decimal ConfidenceLevel { get; set; }
    
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Single forecast data point
/// </summary>
public class ForecastDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal PredictedConsumption { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
}
