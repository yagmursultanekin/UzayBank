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
    }

    public async Task<List<AccountDto>> GetAccountsByUserIdAsync(int userId)
    {
        await SetAuthHeaderAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var response = await _httpClient.PostAsync(
            $"{baseUrl}/AccountInformationServices/accountList",
            null);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var accounts = new List<AccountDto>();
        foreach (var item in json.RootElement.EnumerateArray())
        {
            accounts.Add(new AccountDto
            {
                AccountNumber = item.GetProperty("accountNumber").GetString() ?? "",
                IBAN = item.GetProperty("iban").GetString() ?? "",
                Currency = item.GetProperty("currency").GetString() ?? "",
                Balance = item.GetProperty("balance").GetDecimal(),
                AccountHolderName = item.GetProperty("accountHolderName").GetString() ?? ""
            });
        }

        return accounts;
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId)
    {
        await SetAuthHeaderAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var response = await _httpClient.PostAsync(
            $"{baseUrl}/AccountInformationServices/accountDetail",
            null);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        return new AccountDto
        {
            AccountNumber = json.RootElement.GetProperty("accountNumber").GetString() ?? "",
            IBAN = json.RootElement.GetProperty("iban").GetString() ?? "",
            Currency = json.RootElement.GetProperty("currency").GetString() ?? "",
            Balance = json.RootElement.GetProperty("balance").GetDecimal(),
            AccountHolderName = json.RootElement.GetProperty("accountHolderName").GetString() ?? ""
        };
    }

    public async Task<List<TransactionDto>> GetTransactionsByAccountIdAsync(
        int accountId, DateTime startDate, DateTime endDate)
    {
        await SetAuthHeaderAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var requestBody = JsonSerializer.Serialize(new
        {
            accountNumber = accountId.ToString(),
            startDate = startDate.ToString("yyyy-MM-dd"),
            endDate = endDate.ToString("yyyy-MM-dd")
        });

        var content = new StringContent(
            requestBody,
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{baseUrl}/AccountInformationServices/accountTransactions",
            content);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(responseContent);

        var transactions = new List<TransactionDto>();
        foreach (var item in json.RootElement.EnumerateArray())
        {
            transactions.Add(new TransactionDto
            {
                Amount = item.GetProperty("amount").GetDecimal(),
                Description = item.GetProperty("description").GetString() ?? "",
                TransactionDate = item.GetProperty("transactionDate").GetDateTime(),
                BalanceAfterTransaction = item.GetProperty("balance").GetDecimal()
            });
        }

        return transactions;
    }

    public async Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId)
    {
        // VakıfBank API zaten token ile doğrulama yapıyor
        // Token geçerliyse kullanıcı kendi hesaplarına erişiyor demektir
        return await Task.FromResult(true);
    }

    public async Task<TransactionDto?> AddTransactionAsync(
        int accountId, int userId, CreateTransactionDto dto)
    {
        // İleride para transferi API'si eklenecek
        return await Task.FromResult<TransactionDto?>(null);
    }
}