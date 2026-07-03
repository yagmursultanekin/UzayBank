using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NexBank.Infrastructure.ExternalServices;

public class VakifBankAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private string? _cachedToken;
    private DateTime _tokenExpiry;

    public VakifBankAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // Token hâlâ geçerliyse cache'den dön
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var apiKey = _configuration["VakifBankApi:ApiKey"];
        var apiSecret = _configuration["VakifBankApi:ApiSecret"];
        var scope = _configuration["VakifBankApi:Scope"];

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", apiKey!),
            new KeyValuePair<string, string>("client_secret", apiSecret!),
            new KeyValuePair<string, string>("scope", scope!)
        });

        var baseUrl = _configuration["VakifBankApi:BaseUrl"];
        var response = await _httpClient.PostAsync($"{baseUrl}/token", formData);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        _cachedToken = json.RootElement.GetProperty("access_token").GetString();
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);

        return _cachedToken!;
    }
}