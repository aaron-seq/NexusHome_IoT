using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusHome.IoT.Core.Domain;

public class BatteryStatus : BaseEntity
{
    public int SmartHomeDeviceId { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal ChargeLevelPercentage { get; set; }
    
    [Column(TypeName = "decimal(10,4)")]
    public decimal VoltageReading { get; set; }
    
    [Column(TypeName = "decimal(10,4)")]
    public decimal CurrentReading { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal TemperatureCelsius { get; set; }
    
    public BatteryOperationMode OperationMode { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal StateOfHealthPercentage { get; set; }
    
    public int CycleCount { get; set; }
    
    public DateTime MeasurementTimestamp { get; set; }
    
    // Navigation Properties
    public virtual SmartHomeDevice SmartHomeDevice { get; set; } = null!;
}

public enum BatteryOperationMode
{
    Charging,
    Discharging,
    Idle,
    Maintenance,
    Error
}
