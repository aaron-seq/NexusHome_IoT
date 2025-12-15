using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusHome.IoT.Core.Domain;

public class EnergyOptimizationRule : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string RuleName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public OptimizationRuleType RuleType { get; set; }
    
    public int PriorityLevel { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal TargetSavingsPercentage { get; set; }
    
    public int ComfortImpactThreshold { get; set; }
    
    public string? ConditionsJson { get; set; }
    
    public string? ActionsJson { get; set; }
    
    public DateTime? LastExecutedAt { get; set; }
    
    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAccumulatedSavings { get; set; }
}

public enum OptimizationRuleType
{
    LoadShifting,
    PeakShaving,
    BatteryOptimization,
    ThermalStorage,
    SolarSelfConsumption,
    SmartApplianceScheduling
}
