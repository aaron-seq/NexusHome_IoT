using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusHome.IoT.Infrastructure.Services
{
    public class AutomationRuleProcessorService : BackgroundService
    {
        private readonly ILogger<AutomationRuleProcessorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        public AutomationRuleProcessorService(ILogger<AutomationRuleProcessorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automation Rule Processor Service started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Placeholder for processing rules logic
                     await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Automation Rule Processor Service");
                     await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
    }
}
