using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Application.DTOs;

/// <summary>
/// Data transfer object for smart home device information
/// </summary>
public class DeviceDto
{
    public int Id { get; set; }
    
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Type { get; set; } = string.Empty;
    
    public string? Location { get; set; }
    
    public bool IsOnline { get; set; }
    
    public bool IsActive { get; set; }
    
    public decimal PowerConsumption { get; set; }
    
    public string? Manufacturer { get; set; }
    
    public string? Model { get; set; }
    
    public string? FirmwareVersion { get; set; }
    
    public DateTime LastSeen { get; set; }
}

/// <summary>
/// Request for submitting device telemetry data
/// </summary>
public class DeviceTelemetryRequest
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    public decimal? PowerConsumption { get; set; }
    
    public decimal? Voltage { get; set; }
    
    public decimal? Current { get; set; }
    
    public decimal? Frequency { get; set; }
    
    public decimal? PowerFactor { get; set; }
    
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Energy consumption data point
/// </summary>
public class EnergyConsumptionDto
{
    public DateTime Timestamp { get; set; }
    
    public decimal PowerConsumptionKwh { get; set; }
    
    public decimal Voltage { get; set; }
    
    public decimal Current { get; set; }
    
    public decimal Frequency { get; set; }
    
    public decimal PowerFactor { get; set; }
    
    public decimal CostEstimate { get; set; }
}
