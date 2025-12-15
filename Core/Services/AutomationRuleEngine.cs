using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Core.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace NexusHome.IoT.Core.Services;

public class AutomationRuleEngine : IAutomationRuleEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomationRuleEngine> _logger;

    public AutomationRuleEngine(
        IServiceProvider serviceProvider,
        ILogger<AutomationRuleEngine> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EvaluateRulesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        try
        {
            var rules = await context.AutomationRules
                .Where(r => r.IsEnabled)
                .ToListAsync();

            foreach (var rule in rules)
            {
                await EvaluateRuleAsync(context, rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating automation rules");
        }
    }

    public async Task ExecuteRuleAsync(string ruleId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

        if (int.TryParse(ruleId, out int id))
        {
            var rule = await context.AutomationRules.FindAsync(id);
            if (rule != null)
            {
                await ExecuteActionsAsync(rule);
            }
        }
    }

    private async Task EvaluateRuleAsync(SmartHomeDbContext context, IntelligentAutomationRule rule)
    {
        // Logic to check conditions
        // Stub: Always false for now
        bool conditionMet = false;

        if (conditionMet)
        {
            await ExecuteActionsAsync(rule);
            // Update last executed
            rule.LastExecuted = DateTime.UtcNow;
            rule.ExecutionCount++;
        }
    }

    private async Task ExecuteActionsAsync(IntelligentAutomationRule rule)
    {
        _logger.LogInformation("Executing rule: {RuleId}", rule.Id);
        await Task.CompletedTask;
    }
}
