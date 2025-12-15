using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Infrastructure.Services;

public class UtilityPriceProvider : IUtilityPriceProvider
{
    private readonly ILogger<UtilityPriceProvider> _logger;

    public UtilityPriceProvider(ILogger<UtilityPriceProvider> logger)
    {
        _logger = logger;
    }

    public Task<decimal> GetCurrentElectricityPriceAsync()
    {
        // Mock price
        // Peak hours?
        var hour = DateTime.Now.Hour;
        decimal price = (hour >= 17 && hour <= 21) ? 0.25m : 0.12m;
        return Task.FromResult(price);
    }
}
