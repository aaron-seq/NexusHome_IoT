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
            var predictionService = scope.ServiceProvider.GetRequiredService<NexusHome.IoT.AI.IPredictiveMaintenanceService>(); // Phase 4

            try
            {
                var rules = await context.AutomationRules
                    .Where(r => r.IsEnabled)
                    .ToListAsync();

                foreach (var rule in rules)
                {
                    await EvaluateRuleAsync(context, rule, predictionService);
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

        private async Task EvaluateRuleAsync(
            SmartHomeDbContext context, 
            IntelligentAutomationRule rule,
            NexusHome.IoT.AI.IPredictiveMaintenanceService predictionService)
        {
            bool conditionMet = false;

            // Phase 4: Proactive AI Trigger
            if (rule.TriggerType == "PredictionConfidence" && int.TryParse(rule.TriggerValue, out int threshold))
            {
                // Assuming TriggerSource is DeviceId
                if (int.TryParse(rule.TriggerSource, out int deviceId))
                {
                    var prediction = await predictionService.PredictMaintenanceNeedAsync(deviceId);
                    var confidence = (int)(prediction.FailureProbability * 100);
                    
                    if (confidence >= threshold)
                    {
                        conditionMet = true;
                        _logger.LogInformation("Rule {RuleId} triggered by High Prediction Confidence: {Confidence}%", rule.Id, confidence);
                    }
                }
            }
            else
            {
                // Regular logic (stub)
                conditionMet = false; 
            }

            if (conditionMet)
            {
                await ExecuteActionsAsync(rule);
                rule.LastExecuted = DateTime.UtcNow;
                rule.ExecutionCount++;
                // In real app: Save changes to context
            }
        }

        private async Task ExecuteActionsAsync(IntelligentAutomationRule rule)
        {
            _logger.LogInformation("Executing rule: {RuleId} - {ActionType}", rule.Id, rule.ActionType);
            // Logic to call UniversalBridge or Dispatcher would go here
            await Task.CompletedTask;
        }
    }
