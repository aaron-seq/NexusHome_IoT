namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IWeatherDataProvider
{
    Task<object> GetCurrentWeatherAsync(string location);
}
