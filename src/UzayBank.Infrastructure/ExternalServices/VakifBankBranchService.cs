using Microsoft.Extensions.Configuration;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UzayBank.Infrastructure.ExternalServices;

public class VakifBankBranchService : IBranchService
{
    private readonly HttpClient _httpClient;
    private readonly VakifBankAuthService _authService;
    private readonly IConfiguration _configuration;

    // Türkçe kültür: VakıfBank koordinatları virgül ondalıklı gönderiyor/bekliyor ("41,032575")
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public VakifBankBranchService(
        HttpClient httpClient,
        VakifBankAuthService authService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configuration = configuration;
    }

    public async Task<List<BranchDto>> GetNearestAsync(double latitude, double longitude, int distanceLimitKm)
    {
        var token = await _authService.GetClientCredentialsTokenAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        // Servis, uzun ondalık kuyruklu koordinatı kabul etmiyor (ACBH000049).
        // 6 hane ≈ ~10 cm hassasiyet — ATM araması için fazlasıyla yeterli.
        var requestBody = JsonSerializer.Serialize(new
        {
            Latitude = Math.Round(latitude, 6).ToString(TrCulture),
            Longitude = Math.Round(longitude, 6).ToString(TrCulture),
            DistanceLimit = distanceLimitKm
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/getNearestBranchATM")
        {
            Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Accept", "application/json");

        Console.WriteLine($"VAKIFBANK ISTEK: {request.RequestUri}");
        Console.WriteLine($"VAKIFBANK BODY: {requestBody}");
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"VAKIFBANK CEVAP /getNearestBranchATM ({response.StatusCode}): {content}");

        if (content.TrimStart().StartsWith("<"))
            throw new InvalidOperationException(
                $"VakıfBank WAF isteği reddetti (HTML döndü). Cevap: {content}");

        response.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(content);
        var branches = new List<BranchDto>();

        if (!json.RootElement.TryGetProperty("Data", out var data) ||
            !data.TryGetProperty("BranchandATM", out var listElement))
        {
            return branches;
        }

        // Transactions'tan öğrendiğimiz ders: tek kayıt düz obje dönebilir
        var items = listElement.ValueKind == JsonValueKind.Array
            ? listElement.EnumerateArray().ToList()
            : new List<JsonElement> { listElement };

        var id = 1;
        foreach (var item in items)
        {
            branches.Add(new BranchDto
            {
                Id = id++,
                Name = GetString(item, "Name"),
                Type = GetString(item, "Type"),
                Address = GetString(item, "Address"),
                Phone = GetString(item, "Phone"),
                Latitude = ParseTrDouble(GetString(item, "Latitude")),
                Longitude = ParseTrDouble(GetString(item, "Longitude")),
                DistanceKm = item.TryGetProperty("Distance", out var d) &&
                             d.ValueKind == JsonValueKind.Number ? d.GetDouble() : 0
            });
        }

        // Servis zaten mesafeye göre sıralı dönüyor ama garantiye alalım
        return branches.OrderBy(b => b.DistanceKm).ToList();
    }

    private static string GetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) &&
               prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : "";
    }

    private static double ParseTrDouble(string value)
    {
        // "41,03289794900" → 41.032897949
        return double.TryParse(value, NumberStyles.Any, TrCulture, out var result) ? result : 0;
    }
}