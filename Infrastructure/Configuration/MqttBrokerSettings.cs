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

/// <summary>
/// Configuration settings for JWT authentication in the IoT platform
/// Provides secure token-based authentication for API access and device communication
/// </summary>
public class JwtAuthenticationSettings
{
    /// <summary>
    /// Secret key used for JWT token signing and verification
    /// Must be at least 256 bits (32 characters) for HS256 algorithm
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "JWT secret key must be at least 32 characters for security")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT token issuer identifier
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Issuer { get; set; } = "NexusHome.IoT";

    /// <summary>
    /// JWT token audience identifier
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Audience { get; set; } = "NexusHome.Clients";

    /// <summary>
    /// Access token expiration time in minutes
    /// </summary>
    [Range(5, 10080)] // 5 minutes to 1 week
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    [Range(1, 365)] // 1 day to 1 year
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Whether to validate the token issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the token audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance in minutes to account for server time differences
    /// </summary>
    [Range(0, 30)]
    public int ClockSkewMinutes { get; set; } = 5;
}

/// <summary>
/// Configuration settings for weather API integration
/// Supports external weather data providers for energy optimization and automation
/// </summary>
public class WeatherApiSettings
{
    /// <summary>
    /// Weather API provider base URL
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5/";

    /// <summary>
    /// API key for weather service authentication
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Geographic location for weather data (latitude,longitude)
    /// </summary>
    [StringLength(50)]
    public string Location { get; set; } = "40.7128,-74.0060"; // New York City default

    /// <summary>
    /// Units for temperature measurements (metric, imperial, kelvin)
    /// </summary>
    [StringLength(20)]
    public string Units { get; set; } = "metric";

    /// <summary>
    /// How often to refresh weather data in minutes
    /// </summary>
    [Range(5, 1440)] // 5 minutes to 24 hours
    public int RefreshIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to include weather forecasts in data retrieval
    /// </summary>
    public bool IncludeForecasts { get; set; } = true;

    /// <summary>
    /// Number of days to retrieve in weather forecasts
    /// </summary>
    [Range(1, 16)]
    public int ForecastDays { get; set; } = 5;
}
