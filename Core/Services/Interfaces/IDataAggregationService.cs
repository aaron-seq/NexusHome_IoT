namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IDataAggregationService
{
    Task AggregateDeviceDataAsync();
    Task GenerateReportsAsync();
}
