namespace NexusHome.IoT.Core.Services.Interfaces
{
    public interface ISmartDeviceManager
    {
        Task ProcessTelemetryDataAsync(DeviceTelemetryRequest request);
    }

    public interface IEnergyConsumptionAnalyzer { }
    public interface IAutomationRuleEngine { }
    public interface IPredictiveMaintenanceEngine { }
    public interface IEnergyOptimizationEngine { }
    public interface INotificationDispatcher { }
    public interface IDataAggregationService { }
    public interface ISecurityManager { }
}
