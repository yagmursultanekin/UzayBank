using NexBank.Application.DTOs;

namespace NexBank.Application.Interfaces;

public interface IMarketService
{
    Task<List<MarketRateDto>> GetCurrencyRatesAsync();
    Task<List<MarketRateDto>> GetGoldPricesAsync();
}