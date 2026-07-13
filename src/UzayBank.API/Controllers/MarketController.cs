using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.Interfaces;

namespace UzayBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketController : ControllerBase
{
    private readonly IMarketService _marketService;

    public MarketController(IMarketService marketService)
    {
        _marketService = marketService;
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencyRates()
    {
        var rates = await _marketService.GetCurrencyRatesAsync();
        return Ok(rates);
    }

    [HttpGet("gold")]
    public async Task<IActionResult> GetGoldPrices()
    {
        var prices = await _marketService.GetGoldPricesAsync();
        return Ok(prices);
    }
}