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
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var apiKey = _configuration["VakifBankApi:ApiKey"];
        var apiSecret = _configuration["VakifBankApi:ApiSecret"];
        var scope = _configuration["VakifBankApi:Scope"];
        var resource = _configuration["VakifBankApi:Resource"];
        var consentId = _configuration["VakifBankApi:consentId"];
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", apiKey!),
            new KeyValuePair<string, string>("client_secret", apiSecret!),
            new KeyValuePair<string, string>("grant_type", "b2b_credentials"),
            new KeyValuePair<string, string>("scope", scope!),
            new KeyValuePair<string, string>("resource", resource!),
            new KeyValuePair<string, string>("consentId", consentId!)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/oauth2/token")
        {
            Content = formData
        };
        request.Headers.Add("User-Agent", "PostmanRuntime/7.39.0");
        request.Headers.Add("Accept", "*/*");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine("VAKIFBANK HATA DETAYI:");
            Console.WriteLine(ex.ToString());
            throw;
        }
        var rawContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"VAKIFBANK TOKEN CEVABI ({response.StatusCode}): {rawContent}");
        response.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(rawContent);
        _cachedToken = json.RootElement.GetProperty("access_token").GetString();
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);

        return _cachedToken!;
    }
}