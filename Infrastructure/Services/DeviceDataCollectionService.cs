using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Services
{
    /// <summary>
    /// Background service that periodically collects data from registered devices
    /// </summary>
    public class DeviceDataCollectionService : BackgroundService
    {
        private readonly ILogger<DeviceDataCollectionService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public DeviceDataCollectionService(ILogger<DeviceDataCollectionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Device Data Collection Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Collecting device data...");
                    
                    // Placeholder: Poll devices for latest data
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Device Data Collection Service is stopping");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Device Data Collection Service");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Device Data Collection Service stopped");
        }
    }
}
