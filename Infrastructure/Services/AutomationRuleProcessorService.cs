using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Infrastructure.Services;

public class AutomationRuleProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomationRuleProcessorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public AutomationRuleProcessorService(
        IServiceProvider serviceProvider,
        ILogger<AutomationRuleProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Automation Rule Processor Service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var ruleEngine = scope.ServiceProvider.GetRequiredService<IAutomationRuleEngine>();
                await ruleEngine.EvaluateRulesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Automation Rule Processor Service");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
