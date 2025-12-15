namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IUtilityPriceProvider
{
    Task<decimal> GetCurrentElectricityPriceAsync();
}
