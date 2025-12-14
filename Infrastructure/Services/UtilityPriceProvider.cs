using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Infrastructure.Services
{
    public class UtilityPriceProvider : IUtilityPriceProvider
    {
        private readonly ILogger<UtilityPriceProvider> _logger;

        public UtilityPriceProvider(ILogger<UtilityPriceProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<decimal> GetCurrentElectricityPriceAsync()
        {
            _logger.LogInformation("Getting current electricity price");
            // Placeholder
            return Task.FromResult(0.15m);
        }
    }
}
