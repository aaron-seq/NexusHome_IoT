using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusHome.IoT.Core.Domain;

public class SolarGeneration : BaseEntity
{
    public int SmartHomeDeviceId { get; set; }
    
    [Column(TypeName = "decimal(12,4)")]
    public decimal PowerGeneratedKilowatts { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal EfficiencyPercentage { get; set; }
    
    public DateTime MeasurementTimestamp { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal TemperatureCelsius { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal IrradianceWattsPerSquareMeter { get; set; }
    
    // Navigation Properties
    public virtual SmartHomeDevice SmartHomeDevice { get; set; } = null!;
}
