using Microsoft.Extensions.Configuration;
using NexBank.Application.DTOs;
using NexBank.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NexBank.Infrastructure.ExternalServices;

public class VakifBankAccountService : IAccountService
{
    private readonly HttpClient _httpClient;
    private readonly VakifBankAuthService _authService;
    private readonly IConfiguration _configuration;

    public VakifBankAccountService(
        HttpClient httpClient,
        VakifBankAuthService authService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configuration = configuration;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<AccountDto>> GetAccountsByUserIdAsync(int userId)
    {
        await SetAuthHeaderAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/accountList")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("User-Agent", "PostmanRuntime/7.39.0");
        request.Headers.Add("Accept", "application/json");
        var token = await _authService.GetAccessTokenAsync();
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _httpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"ACCOUNTLIST CEVABI ({response.StatusCode}): {content}");
        response.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(content);
        var accounts = new List<AccountDto>();

        JsonElement accountsElement;
        if (json.RootElement.ValueKind == JsonValueKind.Array)
        {
            accountsElement = json.RootElement;
        }
        else if (json.RootElement.TryGetProperty("Data", out var data) &&
                 data.TryGetProperty("Accounts", out var accs))
        {
            accountsElement = accs;
        }
        else
        {
            return accounts;
        }

        foreach (var item in accountsElement.EnumerateArray())
        {
            accounts.Add(new AccountDto
            {
                AccountNumber = item.TryGetProperty("AccountNumber", out var an) ? an.GetString() ?? "" : "",
                IBAN = item.TryGetProperty("IBAN", out var ib) ? ib.GetString() ?? "" : "",
                Currency = item.TryGetProperty("CurrencyCode", out var cc) ? cc.GetString() ?? "" : "",
                Balance = item.TryGetProperty("Balance", out var b) ? ParseDecimal(b) : 0,
                AccountHolderName = ""
            });
        }

        return accounts;
    }

    private static decimal ParseDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetDecimal();
        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;
        return 0;
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId)
    {
        var accounts = await GetAccountsByUserIdAsync(0);
        return accounts.FirstOrDefault();
    }

    public async Task<List<TransactionDto>> GetTransactionsByAccountIdAsync(
        int accountId, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new List<TransactionDto>());
    }

    public async Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId)
    {
        return await Task.FromResult(true);
    }

    public async Task<TransactionDto?> AddTransactionAsync(
        int accountId, int userId, CreateTransactionDto dto)
    {
        return await Task.FromResult<TransactionDto?>(null);
    }
}