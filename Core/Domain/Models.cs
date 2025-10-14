using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NexusHome.IoT.Core.Domain;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}

public abstract class AuditableEntity : BaseEntity
{
    [StringLength(100)]
    public string? CreatedByUserId { get; set; }
    
    [StringLength(100)]
    public string? ModifiedByUserId { get; set; }
    
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class SmartHomeDevice : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string UniqueDeviceIdentifier { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string DeviceFriendlyName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? DeviceDescription { get; set; }
    
    [Required]
    public DeviceCategory DeviceType { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ManufacturerName { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string ModelNumber { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string FirmwareVersionNumber { get; set; } = string.Empty;
    
    public CommunicationProtocol ConnectionProtocol { get; set; }
    
    public DeviceOperationalStatus CurrentStatus { get; set; }
    
    [StringLength(200)]
    public string? PhysicalLocation { get; set; }
    
    [StringLength(100)]
    public string? RoomAssignment { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal MaximumPowerRating { get; set; }
    
    [Column(TypeName = "decimal(10,4)")]
    public decimal CurrentPowerConsumption { get; set; }
    
    public bool IsCurrentlyOnline { get; set; }
    
    public DateTime LastCommunicationTime { get; set; } = DateTime.UtcNow;
    
    [StringLength(300)]
    public string? MqttTopicPath { get; set; }
    
    public string? DeviceConfigurationJson { get; set; }
    
    public string? AdditionalMetadataJson { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal OperatingTemperature { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal BatteryLevel { get; set; }
    
    public DeviceSecurityStatus SecurityStatus { get; set; } = DeviceSecurityStatus.Secure;
    
    public int NetworkSignalStrength { get; set; }
    
    public DateTime? NextScheduledMaintenance { get; set; }
    
    // Navigation Properties
    public virtual ICollection<DeviceEnergyConsumption> EnergyConsumptionHistory { get; set; } = new List<DeviceEnergyConsumption>();
    
    public virtual ICollection<DeviceMaintenanceRecord> MaintenanceHistory { get; set; } = new List<DeviceMaintenanceRecord>();
    
    public virtual ICollection<DeviceAlert> GeneratedAlerts { get; set; } = new List<DeviceAlert>();
    
    public virtual ICollection<IntelligentAutomationRule> AssociatedAutomationRules { get; set; } = new List<IntelligentAutomationRule>();
    
    public virtual ICollection<DeviceTelemetryReading> TelemetryReadings { get; set; } = new List<DeviceTelemetryReading>();
}

public enum DeviceCategory
{
    ClimateControl,
    LightingSystem,
    ElectricalSwitch,
    EnergyMonitoring,
    SolarPanelSystem,
    SolarInverterUnit,
    BatteryStorageSystem,
    SmartOutlet,
    MotionDetector,
    DoorAccessSensor,
    WindowSecuritySensor,
    SmartLockSystem,
    SecurityCameraSystem,
    VoiceAssistantDevice,
    AirConditioningUnit,
    WaterHeatingSystem,
    RefrigerationAppliance,
    LaundryWashingMachine,
    ClothesDryingMachine,
    DishwashingAppliance,
    ElectricVehicleCharger,
    HeatPumpSystem,
    ElectricityMeterDevice,
    WeatherMonitoringStation,
    GenericIotDevice
}

public enum CommunicationProtocol
{
    MatterProtocol,
    ZigbeeMesh,
    ZWaveNetwork,
    WiFiConnection,
    BluetoothConnection,
    ThreadNetwork,
    ModbusCommunication,
    BacNetProtocol,
    KnxBusSystem,
    LoRaWideAreaNetwork,
    SunSpecProtocol,
    MqttMessaging
}

public enum DeviceOperationalStatus
{
    ActiveAndRunning,
    InactiveOrIdle,
    UnderMaintenance,
    ErrorState,
    OfflineDisconnected,
    StandbyMode,
    ConfigurationMode
}

public enum DeviceSecurityStatus
{
    Secure,
    PendingUpdate,
    VulnerabilityDetected,
    CompromisedSecurity,
    UnknownStatus
}

public class DeviceEnergyConsumption : BaseEntity
{
    public int SmartHomeDeviceId { get; set; }
    
    [Column(TypeName = "decimal(12,4)")]
    public decimal PowerConsumptionKilowattHours { get; set; }
    
    [Column(TypeName = "decimal(10,4)")]
    public decimal VoltageReading { get; set; }
    
    [Column(TypeName = "decimal(10,4)")]
    public decimal CurrentReading { get; set; }
    
    [Column(TypeName = "decimal(8,4)")]
    public decimal PowerFactorReading { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal FrequencyReading { get; set; }
    
    [Column(TypeName = "decimal(12,2)")]
    public decimal CalculatedCostAmount { get; set; }
    
    public DateTime MeasurementTimestamp { get; set; }
    
    public EnergySourceType EnergySource { get; set; }
    
    [StringLength(100)]
    public string? AppliedTariffRate { get; set; }
    
    [Column(TypeName = "decimal(8,4)")]
    public decimal CarbonFootprintGrams { get; set; }
    
    public EnergyQuality PowerQualityIndicator { get; set; } = EnergyQuality.Normal;
    
    // Navigation Properties
    public virtual SmartHomeDevice SmartHomeDevice { get; set; } = null!;
}

public enum EnergySourceType
{
    ElectricGrid,
    SolarGeneration,
    BatteryStorage,
    WindGeneration,
    GeothermalGeneration,
    HydroGeneration,
    AlternativeSource
}

public enum EnergyQuality
{
    Excellent,
    Normal,
    BelowAverage,
    Poor,
    Critical
}
