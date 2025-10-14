using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NexusHome.IoT.Application.DTOs;

/// <summary>
/// Data transfer object for smart device information used in API responses
/// Provides a clean, serializable representation of device data for client applications
/// </summary>
public class SmartDeviceResponseDto
{
    /// <summary>
    /// Unique database identifier for the device
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique device identifier used by the physical device
    /// </summary>
    [Required]
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the device
    /// </summary>
    [Required]
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the device functionality
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category classification of the device (lighting, climate, security, etc.)
    /// </summary>
    [Required]
    public string DeviceCategory { get; set; } = string.Empty;

    /// <summary>
    /// Device manufacturer name
    /// </summary>
    [Required]
    public string ManufacturerName { get; set; } = string.Empty;

    /// <summary>
    /// Model number or identifier from manufacturer
    /// </summary>
    public string? ModelNumber { get; set; }

    /// <summary>
    /// Current firmware version installed on device
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Communication protocol used by device (WiFi, Zigbee, Z-Wave, etc.)
    /// </summary>
    public string? CommunicationProtocol { get; set; }

    /// <summary>
    /// Current operational status of the device
    /// </summary>
    [Required]
    public string OperationalStatus { get; set; } = string.Empty;

    /// <summary>
    /// Physical location description
    /// </summary>
    public string? PhysicalLocation { get; set; }

    /// <summary>
    /// Room assignment for organizational purposes
    /// </summary>
    public string? AssignedRoom { get; set; }

    /// <summary>
    /// Maximum power rating in watts
    /// </summary>
    public decimal MaximumPowerRatingWatts { get; set; }

    /// <summary>
    /// Current power consumption in watts
    /// </summary>
    public decimal CurrentPowerConsumptionWatts { get; set; }

    /// <summary>
    /// Whether the device is currently online and responding
    /// </summary>
    public bool IsOnlineAndReachable { get; set; }

    /// <summary>
    /// Timestamp of last successful communication with device
    /// </summary>
    public DateTime LastCommunicationTimestamp { get; set; }

    /// <summary>
    /// Operating temperature in Celsius (if supported by device)
    /// </summary>
    public decimal? OperatingTemperatureCelsius { get; set; }

    /// <summary>
    /// Battery level percentage (if device is battery-powered)
    /// </summary>
    public decimal? BatteryLevelPercentage { get; set; }

    /// <summary>
    /// Network signal strength indicator (0-100)
    /// </summary>
    public int? NetworkSignalStrength { get; set; }

    /// <summary>
    /// Current security status of the device
    /// </summary>
    public string SecurityStatus { get; set; } = "Unknown";

    /// <summary>
    /// When the device was first registered in the system
    /// </summary>
    public DateTime DeviceRegistrationDate { get; set; }

    /// <summary>
    /// Last time device information was updated
    /// </summary>
    public DateTime LastUpdatedTimestamp { get; set; }

    /// <summary>
    /// Next scheduled maintenance date (if applicable)
    /// </summary>
    public DateTime? NextScheduledMaintenanceDate { get; set; }

    /// <summary>
    /// Additional metadata about the device as key-value pairs
    /// </summary>
    public Dictionary<string, object>? AdditionalMetadata { get; set; }
}

/// <summary>
/// Data transfer object for creating or updating smart device information
/// Used in POST and PUT API requests to manage device data
/// </summary>
public class SmartDeviceRequestDto
{
    /// <summary>
    /// Unique device identifier used by the physical device
    /// Must be unique across all devices in the system
    /// </summary>
    [Required(ErrorMessage = "Device identifier is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Device identifier must be between 1 and 100 characters")]
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the device
    /// </summary>
    [Required(ErrorMessage = "Friendly name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Friendly name must be between 1 and 200 characters")]
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the device functionality
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Category classification of the device
    /// </summary>
    [Required(ErrorMessage = "Device category is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Device category must be between 1 and 50 characters")]
    public string DeviceCategory { get; set; } = string.Empty;

    /// <summary>
    /// Device manufacturer name
    /// </summary>
    [Required(ErrorMessage = "Manufacturer name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Manufacturer name must be between 1 and 100 characters")]
    public string ManufacturerName { get; set; } = string.Empty;

    /// <summary>
    /// Model number or identifier from manufacturer
    /// </summary>
    [StringLength(100, ErrorMessage = "Model number cannot exceed 100 characters")]
    public string? ModelNumber { get; set; }

    /// <summary>
    /// Current firmware version installed on device
    /// </summary>
    [StringLength(50, ErrorMessage = "Firmware version cannot exceed 50 characters")]
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Communication protocol used by device
    /// </summary>
    [StringLength(50, ErrorMessage = "Communication protocol cannot exceed 50 characters")]
    public string? CommunicationProtocol { get; set; }

    /// <summary>
    /// Physical location description
    /// </summary>
    [StringLength(200, ErrorMessage = "Physical location cannot exceed 200 characters")]
    public string? PhysicalLocation { get; set; }

    /// <summary>
    /// Room assignment for organizational purposes
    /// </summary>
    [StringLength(100, ErrorMessage = "Room assignment cannot exceed 100 characters")]
    public string? AssignedRoom { get; set; }

    /// <summary>
    /// Maximum power rating in watts
    /// </summary>
    [Range(0, 100000, ErrorMessage = "Maximum power rating must be between 0 and 100,000 watts")]
    public decimal MaximumPowerRatingWatts { get; set; }

    /// <summary>
    /// Network configuration details (IP address, MAC address, etc.)
    /// </summary>
    public DeviceNetworkConfigurationDto? NetworkConfiguration { get; set; }

    /// <summary>
    /// MQTT topic configuration for device communication
    /// </summary>
    public DeviceMqttConfigurationDto? MqttConfiguration { get; set; }

    /// <summary>
    /// Additional metadata about the device as key-value pairs
    /// </summary>
    public Dictionary<string, object>? AdditionalMetadata { get; set; }
}

/// <summary>
/// Network configuration details for smart devices
/// </summary>
public class DeviceNetworkConfigurationDto
{
    /// <summary>
    /// IP address assigned to the device (IPv4 or IPv6)
    /// </summary>
    [StringLength(45, ErrorMessage = "IP address cannot exceed 45 characters")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// MAC address for network identification
    /// </summary>
    [StringLength(17, ErrorMessage = "MAC address should be in XX:XX:XX:XX:XX:XX format")]
    [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", 
        ErrorMessage = "MAC address must be in XX:XX:XX:XX:XX:XX format")]
    public string? MacAddress { get; set; }

    /// <summary>
    /// Network subnet mask
    /// </summary>
    [StringLength(15, ErrorMessage = "Subnet mask cannot exceed 15 characters")]
    public string? SubnetMask { get; set; }

    /// <summary>
    /// Default gateway IP address
    /// </summary>
    [StringLength(45, ErrorMessage = "Gateway address cannot exceed 45 characters")]
    public string? GatewayAddress { get; set; }

    /// <summary>
    /// DNS server addresses
    /// </summary>
    public List<string>? DnsServers { get; set; }

    /// <summary>
    /// Whether the device uses DHCP for IP assignment
    /// </summary>
    public bool UsesDynamicIpAssignment { get; set; } = true;
}

/// <summary>
/// MQTT configuration details for device communication
/// </summary>
public class DeviceMqttConfigurationDto
{
    /// <summary>
    /// Base MQTT topic path for this device
    /// </summary>
    [Required(ErrorMessage = "MQTT topic path is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "MQTT topic path must be between 1 and 300 characters")]
    public string TopicPath { get; set; } = string.Empty;

    /// <summary>
    /// Quality of Service level for device MQTT messages (0, 1, or 2)
    /// </summary>
    [Range(0, 2, ErrorMessage = "QoS level must be 0, 1, or 2")]
    public int QualityOfServiceLevel { get; set; } = 1;

    /// <summary>
    /// Whether device messages should be retained by the broker
    /// </summary>
    public bool RetainMessages { get; set; } = false;

    /// <summary>
    /// Custom MQTT client ID for this device (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "MQTT client ID cannot exceed 100 characters")]
    public string? CustomClientId { get; set; }

    /// <summary>
    /// Interval in seconds for device heartbeat messages
    /// </summary>
    [Range(10, 3600, ErrorMessage = "Heartbeat interval must be between 10 and 3600 seconds")]
    public int HeartbeatIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// Data transfer object for device telemetry data submission
/// Used when devices or external systems submit sensor readings and operational data
/// </summary>
public class DeviceTelemetrySubmissionDto
{
    /// <summary>
    /// Unique identifier of the device submitting telemetry
    /// </summary>
    [Required(ErrorMessage = "Device identifier is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Device identifier must be between 1 and 100 characters")]
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the telemetry data was captured by the device
    /// </summary>
    [Required(ErrorMessage = "Timestamp is required")]
    public DateTime TelemetryTimestamp { get; set; }

    /// <summary>
    /// Structured sensor readings and operational data
    /// </summary>
    [Required(ErrorMessage = "Sensor data is required")]
    public Dictionary<string, object> SensorReadings { get; set; } = new();

    /// <summary>
    /// Current power consumption reading in watts (if applicable)
    /// </summary>
    [Range(0, 100000, ErrorMessage = "Power consumption must be between 0 and 100,000 watts")]
    public decimal? CurrentPowerConsumptionWatts { get; set; }

    /// <summary>
    /// Device operating temperature in Celsius (if available)
    /// </summary>
    [Range(-50, 200, ErrorMessage = "Operating temperature must be between -50 and 200 degrees Celsius")]
    public decimal? OperatingTemperatureCelsius { get; set; }

    /// <summary>
    /// Battery level percentage for battery-powered devices
    /// </summary>
    [Range(0, 100, ErrorMessage = "Battery level must be between 0 and 100 percent")]
    public decimal? BatteryLevelPercentage { get; set; }

    /// <summary>
    /// Network signal strength indicator (0-100)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Signal strength must be between 0 and 100")]
    public int? NetworkSignalStrength { get; set; }

    /// <summary>
    /// Device operational status at time of telemetry capture
    /// </summary>
    [StringLength(50, ErrorMessage = "Device status cannot exceed 50 characters")]
    public string? DeviceStatus { get; set; }

    /// <summary>
    /// Any error codes or diagnostic information from the device
    /// </summary>
    public List<string>? ErrorCodes { get; set; }

    /// <summary>
    /// Data quality indicator for the submitted telemetry
    /// </summary>
    [StringLength(20, ErrorMessage = "Data quality indicator cannot exceed 20 characters")]
    public string DataQualityIndicator { get; set; } = "Good";
}

/// <summary>
/// Data transfer object for device command requests
/// Used to send operational commands to smart devices
/// </summary>
public class DeviceCommandRequestDto
{
    /// <summary>
    /// Unique identifier of the target device
    /// </summary>
    [Required(ErrorMessage = "Device identifier is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Device identifier must be between 1 and 100 characters")]
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Type of command to execute (power_on, power_off, set_temperature, etc.)
    /// </summary>
    [Required(ErrorMessage = "Command type is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Command type must be between 1 and 50 characters")]
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// Command-specific parameters as key-value pairs
    /// </summary>
    public Dictionary<string, object>? CommandParameters { get; set; }

    /// <summary>
    /// Priority level for command execution (low, normal, high, critical)
    /// </summary>
    [StringLength(20, ErrorMessage = "Priority level cannot exceed 20 characters")]
    public string PriorityLevel { get; set; } = "normal";

    /// <summary>
    /// Maximum time in seconds to wait for command acknowledgment
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to wait for command completion confirmation
    /// </summary>
    public bool RequireConfirmation { get; set; } = true;

    /// <summary>
    /// Optional scheduling information for delayed command execution
    /// </summary>
    public CommandSchedulingDto? SchedulingInfo { get; set; }
}

/// <summary>
/// Scheduling information for delayed command execution
/// </summary>
public class CommandSchedulingDto
{
    /// <summary>
    /// When to execute the command (if not immediate)
    /// </summary>
    public DateTime? ScheduledExecutionTime { get; set; }

    /// <summary>
    /// Whether this is a recurring command
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Recurrence pattern (if recurring)
    /// </summary>
    [StringLength(100, ErrorMessage = "Recurrence pattern cannot exceed 100 characters")]
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// When to stop recurring executions
    /// </summary>
    public DateTime? RecurrenceEndDate { get; set; }
}
