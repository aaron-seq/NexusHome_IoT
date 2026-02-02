using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace NexusHome.IoT.Core.Services;

/// <summary>
/// Manages the "Shadow State" (Device Twin) for all devices.
/// Persists state to Redis to ensure resilience across restarts.
/// Structure:
/// - Desired: What the user/system wants the device to be.
/// - Reported: What the device actually is.
/// </summary>
public class DeviceShadowService
{
    private readonly ILogger<DeviceShadowService> _logger;
    private readonly IConnectionMultiplexer _redis;
    private const string KeyPrefix = "device:shadow:";

    public DeviceShadowService(ILogger<DeviceShadowService> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    /// <summary>
    /// Updates the "Reported" state (from the Device).
    /// </summary>
    public async Task UpdateReportedStateAsync(string deviceId, Dictionary<string, object> properties)
    {
        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{deviceId}";
        
        // We store reported properties in a Hash field "reported"
        var json = JsonSerializer.Serialize(properties);
        await db.HashSetAsync(key, "reported", json);
        await db.HashSetAsync(key, "lastUpdated", DateTime.UtcNow.ToString("o"));
        
        _logger.LogDebug("Updated shadow reported state for {DeviceId}", deviceId);
    }

    /// <summary>
    /// Updates the "Desired" state (from the UI/Automation).
    /// </summary>
    public async Task UpdateDesiredStateAsync(string deviceId, Dictionary<string, object> properties)
    {
        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{deviceId}";

        var json = JsonSerializer.Serialize(properties);
        await db.HashSetAsync(key, "desired", json);
        
        _logger.LogInformation("Updated shadow desired state for {DeviceId}: {State}", deviceId, json);
    }

    /// <summary>
    /// Gets the full shadow state (Reported + Desired).
    /// </summary>
    public async Task<(Dictionary<string, object> Reported, Dictionary<string, object> Desired)> GetShadowAsync(string deviceId)
    {
        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{deviceId}";

        var reportedJson = await db.HashGetAsync(key, "reported");
        var desiredJson = await db.HashGetAsync(key, "desired");

        var reported = reportedJson.HasValue 
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(reportedJson!) 
            : new Dictionary<string, object>();

        var desired = desiredJson.HasValue 
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(desiredJson!) 
            : new Dictionary<string, object>();

        return (reported ?? new(), desired ?? new());
    }
}
