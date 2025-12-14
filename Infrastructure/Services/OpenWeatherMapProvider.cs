using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Infrastructure.Services
{
    public class OpenWeatherMapProvider : IWeatherDataProvider
    {
        private readonly ILogger<OpenWeatherMapProvider> _logger;

        public OpenWeatherMapProvider(ILogger<OpenWeatherMapProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<object> GetCurrentWeatherAsync(string location)
        {
            _logger.LogInformation("Getting current weather for {Location}", location);
            // Placeholder
            return Task.FromResult<object>(new { temp = 20, condition = "Sunny" });
        }
    }
}
