using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace UzayBank.Infrastructure.ExternalServices;

public class VakifBankAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // İki ayrı token türü için ayrı önbellekler:
    // B2B (hesap servisleri) ve Client Credentials (public servisler: ATM/şube)
    private string? _cachedB2bToken;
    private DateTime _b2bTokenExpiry;
    private string? _cachedClientToken;
    private DateTime _clientTokenExpiry;

    public VakifBankAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    /// <summary>Hesap servisleri için B2B Credentials token'ı (consentId gerektirir).</summary>
    public async Task<string> GetAccessTokenAsync()
    {
        if (_cachedB2bToken != null && DateTime.UtcNow < _b2bTokenExpiry)
            return _cachedB2bToken;

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["VakifBankApi:ApiKey"]!),
            new KeyValuePair<string, string>("client_secret", _configuration["VakifBankApi:ApiSecret"]!),
            new KeyValuePair<string, string>("grant_type", "b2b_credentials"),
            new KeyValuePair<string, string>("scope", _configuration["VakifBankApi:Scope"]!),
            new KeyValuePair<string, string>("resource", _configuration["VakifBankApi:Resource"]!),
            new KeyValuePair<string, string>("consentId", _configuration["VakifBankApi:ConsentId"]!)
        });

        var (token, expiry) = await RequestTokenAsync(formData, "B2B");
        _cachedB2bToken = token;
        _b2bTokenExpiry = expiry;
        return token;
    }

    /// <summary>Public servisler (ATM/şube) için Client Credentials token'ı (consentId gerektirmez).</summary>
    public async Task<string> GetClientCredentialsTokenAsync()
    {
        if (_cachedClientToken != null && DateTime.UtcNow < _clientTokenExpiry)
            return _cachedClientToken;

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["VakifBankApi:ApiKey"]!),
            new KeyValuePair<string, string>("client_secret", _configuration["VakifBankApi:ApiSecret"]!),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "oob"),
            new KeyValuePair<string, string>("resource", _configuration["VakifBankApi:Resource"]!)
        });

        var (token, expiry) = await RequestTokenAsync(formData, "CLIENT");
        _cachedClientToken = token;
        _clientTokenExpiry = expiry;
        return token;
    }

    private async Task<(string Token, DateTime Expiry)> RequestTokenAsync(FormUrlEncodedContent formData, string label)
    {
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/oauth2/token")
        {
            Content = formData
        };
        request.Headers.Add("Accept", "*/*");

        var response = await _httpClient.SendAsync(request);
        var rawContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"VAKIFBANK TOKEN CEVABI [{label}] ({response.StatusCode}): {rawContent}");
        response.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(rawContent);
        var token = json.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

        return (token, DateTime.UtcNow.AddSeconds(expiresIn - 30));
    }
}