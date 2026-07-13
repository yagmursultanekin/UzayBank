using UzayBank.Application.DTOs;

namespace UzayBank.Application.Interfaces;

public interface IMarketService
{
    Task<List<MarketRateDto>> GetCurrencyRatesAsync();
    Task<List<MarketRateDto>> GetGoldPricesAsync();
}