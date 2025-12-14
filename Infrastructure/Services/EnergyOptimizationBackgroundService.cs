using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Services
{
    public class EnergyOptimizationBackgroundService : BackgroundService
    {
        private readonly ILogger<EnergyOptimizationBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

        public EnergyOptimizationBackgroundService(ILogger<EnergyOptimizationBackgroundService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Energy Optimization Service started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running energy optimization algorithms...");
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Energy Optimization Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}
