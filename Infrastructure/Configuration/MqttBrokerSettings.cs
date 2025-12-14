using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for MQTT broker connection and communication
/// Provides comprehensive options for connecting to local or cloud-based MQTT brokers
/// </summary>
public class MqttBrokerSettings
{
    /// <summary>
    /// MQTT broker hostname or IP address
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// MQTT broker port number (standard: 1883 for non-TLS, 8883 for TLS)
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Username for MQTT broker authentication (optional)
    /// </summary>
    [StringLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// Password for MQTT broker authentication (optional)
    /// </summary>
    [StringLength(255)]
    public string? Password { get; set; }

    /// <summary>
    /// Unique client identifier for MQTT connection
    /// Must be unique across all connected clients
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ClientId { get; set; } = "NexusHome-IoT-Platform";

    /// <summary>
    /// Keep-alive period in seconds for MQTT connection health monitoring
    /// </summary>
    [Range(10, 3600)]
    public int KeepAlivePeriod { get; set; } = 60;

    /// <summary>
    /// Whether to start with a clean session (no persistent subscriptions)
    /// </summary>
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// Whether to use TLS/SSL encryption for MQTT connection
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of message retries for QoS 1 and 2 messages
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between reconnection attempts in seconds
    /// </summary>
    [Range(1, 300)]
    public int ReconnectDelaySeconds { get; set; } = 10;

    /// <summary>
    /// Predefined topic patterns for different message types
    /// </summary>
    public MqttTopicConfiguration Topics { get; set; } = new();

    /// <summary>
    /// Whether to enable automatic reconnection on connection loss
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum number of pending messages to queue when disconnected
    /// </summary>
    [Range(100, 50000)]
    public int MaxPendingMessages { get; set; } = 10000;

    /// <summary>
    /// Whether to log detailed MQTT protocol messages for debugging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// Configuration for structured MQTT topic hierarchy used by the IoT platform
/// Defines consistent topic patterns for different types of device communication
/// </summary>
public class MqttTopicConfiguration
{
    /// <summary>
    /// Topic pattern for device telemetry data publishing
    /// Default: nexushome/devices/{deviceId}/telemetry
    /// </summary>
    [Required]
    public string DeviceTelemetry { get; set; } = "nexushome/devices/+/telemetry";

    /// <summary>
    /// Topic pattern for device status updates
    /// Default: nexushome/devices/{deviceId}/status
    /// </summary>
    [Required]
    public string DeviceStatus { get; set; } = "nexushome/devices/+/status";

    /// <summary>
    /// Topic pattern for sending commands to devices
    /// Default: nexushome/devices/{deviceId}/commands
    /// </summary>
    [Required]
    public string DeviceCommands { get; set; } = "nexushome/devices/+/commands";

    /// <summary>
    /// Topic pattern for system-wide alerts and notifications
    /// Default: nexushome/system/alerts
    /// </summary>
    [Required]
    public string SystemAlerts { get; set; } = "nexushome/system/alerts";

    /// <summary>
    /// Topic pattern for energy consumption data
    /// Default: nexushome/energy/{deviceId}/data
    /// </summary>
    [Required]
    public string EnergyData { get; set; } = "nexushome/energy/+/data";

    /// <summary>
    /// Topic pattern for automation rule execution notifications
    /// Default: nexushome/automation/rules/executed
    /// </summary>
    [Required]
    public string AutomationExecution { get; set; } = "nexushome/automation/rules/executed";

    /// <summary>
    /// Topic pattern for maintenance alert notifications
    /// Default: nexushome/maintenance/{deviceId}/alerts
    /// </summary>
    [Required]
    public string MaintenanceAlerts { get; set; } = "nexushome/maintenance/+/alerts";

    /// <summary>
    /// Topic pattern for security-related events and alerts
    /// Default: nexushome/security/events
    /// </summary>
    [Required]
    public string SecurityEvents { get; set; } = "nexushome/security/events";

    /// <summary>
    /// Topic pattern for weather data integration
    /// Default: nexushome/weather/data
    /// </summary>
    [Required]
    public string WeatherData { get; set; } = "nexushome/weather/data";

    /// <summary>
    /// Topic pattern for system health and diagnostics
    /// Default: nexushome/system/health
    /// </summary>
    [Required]
    public string SystemHealth { get; set; } = "nexushome/system/health";
}


