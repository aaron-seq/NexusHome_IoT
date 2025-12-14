using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusHome.IoT.Infrastructure.Services;

/// <summary>
/// Background service responsible for initiating and maintaining the MQTT connection.
/// Ensures the enhanced MQTT client connects without blocking application startup.
/// </summary>
public class MqttConnectionService : BackgroundService
{
    private readonly IMqttClientService _mqttClientService;
    private readonly ILogger<MqttConnectionService> _logger;

    public MqttConnectionService(IMqttClientService mqttClientService, ILogger<MqttConnectionService> logger)
    {
        _mqttClientService = mqttClientService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing MQTT connection service...");

        try
        {
            // Initial connection attempt
            // The EnhancedMqttClientService handles its own reconnect logic once started,
            // but we need to trigger the initial ConnectAsync.
            await _mqttClientService.ConnectAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MQTT connection service stopped during startup.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MQTT connection during background startup.");
            // We don't throw here to avoid crashing the background host, 
            // relying on the service's internal retry mechanisms or subsequent checks.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MQTT connection service...");
        await _mqttClientService.DisconnectAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
