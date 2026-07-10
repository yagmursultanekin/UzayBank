using Microsoft.Extensions.Configuration;
using NexBank.Application.DTOs;
using NexBank.Application.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NexBank.Infrastructure.ExternalServices;

public class VakifBankMarketService : IMarketService
{
    private readonly HttpClient _httpClient;
    private readonly VakifBankAuthService _authService;
    private readonly IConfiguration _configuration;

    public VakifBankMarketService(
        HttpClient httpClient,
        VakifBankAuthService authService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configuration = configuration;
    }

    public async Task<List<MarketRateDto>> GetCurrencyRatesAsync()
    {
        var content = await PostAsync("/getCurrencyRates", dateFieldName: "ValidityDate");
        return ParseRates(content, listName: "Currency",
            codeField: "CurrencyCode", nameField: "CurrencyName");
    }

    public async Task<List<MarketRateDto>> GetGoldPricesAsync()
    {
        var content = await PostAsync("/getGoldPrices", dateFieldName: "PriceDate");
        return ParseRates(content, listName: "GoldRate",
            codeField: "ISIN", nameField: "ProductName");
    }

   private async Task<string> PostAsync(string endpoint, string dateFieldName)
    {
        var token = await _authService.GetClientCredentialsTokenAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        // Servisler boş body kabul etmiyor; günün tarihiyle sorguluyoruz.
        // Not: tarih alanının adı servise göre değişiyor (PriceDate / ValidityDate)
        /* var dateText = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
         var requestBody = $"{{ \"{dateFieldName}\": \"{dateText}\" }}"; */

        // Türkiye saatiyle (+03:00) o anın damgası — servisin "bugün" tanımıyla uyumlu
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));
        var dateText = turkeyTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff") + "+03:00";
        var requestBody = $"{{ \"{dateFieldName}\": \"{dateText}\" }}";
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{endpoint}")
        {
            Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Accept", "application/json");

        Console.WriteLine($"VAKIFBANK ISTEK: {request.RequestUri}");
        Console.WriteLine($"VAKIFBANK BODY: {requestBody}");
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"VAKIFBANK CEVAP {endpoint} ({response.StatusCode}): {content}");

        if (content.TrimStart().StartsWith("<"))
            throw new InvalidOperationException(
                $"VakıfBank WAF isteği reddetti (HTML döndü). Cevap: {content}");

        response.EnsureSuccessStatusCode();
        return content;
    }

    private static List<MarketRateDto> ParseRates(string content, string listName, string codeField, string nameField)
    {
        var json = JsonDocument.Parse(content);
        var rates = new List<MarketRateDto>();

        if (!json.RootElement.TryGetProperty("Data", out var data) ||
            !data.TryGetProperty(listName, out var listElement))
        {
            return rates;
        }

        var items = listElement.ValueKind == JsonValueKind.Array
            ? listElement.EnumerateArray().ToList()
            : new List<JsonElement> { listElement };

        foreach (var item in items)
        {
            var sale = ParseInvariantDecimal(GetString(item, "SaleRate"));
            var purchase = ParseInvariantDecimal(GetString(item, "PurchaseRate"));

            // Sandbox'ta verisi girilmemiş kayıtlar 0.0000 dönüyor — bunları eliyoruz
            if (sale == 0 && purchase == 0)
                continue;

            rates.Add(new MarketRateDto
            {
                Code = GetString(item, codeField),
                Name = GetString(item, nameField),
                SaleRate = sale,
                PurchaseRate = purchase,
                RateDate = DateTime.TryParse(GetString(item, "RateDate"), out var dt)
                    ? dt : DateTime.MinValue
            });
        }

        return rates;
    }

    private static string GetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) &&
               prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : "";
    }

    private static decimal ParseInvariantDecimal(string value)
    {
        // Kur servisleri fiyatları NOKTALI string dönüyor ("46.3562") —
        // koordinatların aksine (virgüllü). InvariantCulture ile parse ediyoruz.
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result : 0;
    }
}