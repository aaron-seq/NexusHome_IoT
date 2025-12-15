using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Core.Domain; // If needed for WeatherData

namespace NexusHome.IoT.Infrastructure.Services;

public class OpenWeatherMapProvider : IWeatherDataProvider
{
    private readonly ILogger<OpenWeatherMapProvider> _logger;

    public OpenWeatherMapProvider(ILogger<OpenWeatherMapProvider> logger)
    {
        _logger = logger;
    }

    public Task<object> GetCurrentWeatherAsync(string location)
    {
        // Mock response
        return Task.FromResult<object>(new 
        { 
            Temperature = 22.5, 
            Condition = "Cloudy", 
            Humidity = 45 
        });
    }
}
