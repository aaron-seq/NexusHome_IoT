using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Infrastructure.Configuration;

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
