namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IAutomationRuleEngine
{
    Task EvaluateRulesAsync();
    Task ExecuteRuleAsync(string ruleId);
}
