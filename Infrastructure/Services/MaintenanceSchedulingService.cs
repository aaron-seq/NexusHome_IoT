using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Services
{
    public class MaintenanceSchedulingService : BackgroundService
    {
        private readonly ILogger<MaintenanceSchedulingService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public MaintenanceSchedulingService(ILogger<MaintenanceSchedulingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Maintenance Scheduling Service started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking maintenance schedules...");
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Maintenance Scheduling Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}
