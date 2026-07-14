using Microsoft.Extensions.Configuration;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Enums;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UzayBank.Infrastructure.ExternalServices;

public class VakifBankAccountService : IAccountService
{
    private readonly HttpClient _httpClient;
    private readonly VakifBankAuthService _authService;
    private readonly IConfiguration _configuration;

    // Hesap listesi önbelleği — istekler arası paylaşılır (servis scoped olduğu için static)
    private static List<AccountDto>? _cachedAccounts;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly object _cacheLock = new();
    public VakifBankAccountService(
        HttpClient httpClient,
        VakifBankAuthService authService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configuration = configuration;
    }

    // Tüm VakıfBank çağrıları için ortak POST metodu
    private async Task<string> PostAsync(string endpoint, string jsonBody)
    {
        var token = await _authService.GetAccessTokenAsync();
        var baseUrl = _configuration["VakifBankApi:BaseUrl"];

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{endpoint}")
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Accept", "application/json");

        Console.WriteLine($"VAKIFBANK ISTEK: {request.RequestUri}");
        Console.WriteLine($"VAKIFBANK BODY: {jsonBody}");
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"VAKIFBANK CEVAP {endpoint} ({response.StatusCode}): {content}");

        if (content.TrimStart().StartsWith("<"))
            throw new InvalidOperationException(
                $"VakıfBank WAF isteği reddetti (HTML döndü). Cevap: {content}");

        if (!response.IsSuccessStatusCode)
            throw new VakifBankApiException(response.StatusCode, content);

        return content;
    }

    public async Task<List<AccountDto>> GetAccountsByUserIdAsync(int userId)
    {
        // Önbellek taze ise API'ye hiç gitme
        lock (_cacheLock)
        {
            if (_cachedAccounts != null && DateTime.UtcNow < _cacheExpiry)
                return _cachedAccounts;
        }

        var content = await PostAsync("/accountList", "{}");

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

        var id = 1;
        foreach (var item in accountsElement.EnumerateArray())
        {
            accounts.Add(new AccountDto
            {
                Id = id++, // Liste sırasından üretilen kimlik — controller bu Id ile çalışıyor
                AccountNumber = item.TryGetProperty("AccountNumber", out var an) ? an.GetString() ?? "" : "",
                IBAN = item.TryGetProperty("IBAN", out var ib) ? ib.GetString() ?? "" : "",
                Currency = item.TryGetProperty("CurrencyCode", out var cc) ? cc.GetString() ?? "" : "",
                Balance = item.TryGetProperty("Balance", out var b) ? ParseDecimal(b) : 0,
                AccountHolderName = ""
            });
        }

        lock (_cacheLock)
        {
            _cachedAccounts = accounts;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(2);
        }

        return accounts;
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId)
    {
        var accounts = await GetAccountsByUserIdAsync(0);
        Console.WriteLine($"HESAP ARAMA: istenen Id={accountId}, listedeki Id'ler: [{string.Join(", ", accounts.Select(a => a.Id))}]");
        return accounts.FirstOrDefault(a => a.Id == accountId);
    }

    public async Task<List<TransactionDto>> GetTransactionsByAccountIdAsync(
        int accountId, DateTime startDate, DateTime endDate)
    {
        // Önce hesabı bul, VakıfBank'ın beklediği AccountNumber'a çevir
        var account = await GetAccountByIdAsync(accountId);
        if (account == null)
            return new List<TransactionDto>();

        var requestBody = JsonSerializer.Serialize(new
        {
            AccountNumber = account.AccountNumber,
            StartDate = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            EndDate = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        string content;
        try
        {
            content = await PostAsync("/accountTransactions", requestBody);
        }
        catch (VakifBankApiException ex) when (ex.HasErrorCode("ACBH000202"))
        {
            // Tarih aralığında hareket yok — hata değil, boş sonuç.
            // Diğer tüm hatalar (rıza geçersiz, yetki, sunucu) yukarı fırlar.
            return new List<TransactionDto>();
        }

        var json = JsonDocument.Parse(content);
        var transactions = new List<TransactionDto>();

        if (!json.RootElement.TryGetProperty("Data", out var data) ||
            !data.TryGetProperty("AccountTransactions", out var txElement))
        {
            return transactions;
        }

        // Tek işlem düz obje, birden fazla işlem dizi olarak dönebiliyor — ikisini de karşıla
        var items = txElement.ValueKind == JsonValueKind.Array
            ? txElement.EnumerateArray().ToList()
            : new List<JsonElement> { txElement };

        var txId = 1;
        foreach (var item in items)
        {
            transactions.Add(new TransactionDto
            {
                ID = txId++,
                Amount = item.TryGetProperty("Amount", out var amt) ? ParseDecimal(amt) : 0,
                Type = item.TryGetProperty("TransactionType", out var tt) && tt.GetString() == "2"
                    ? TransactionType.Debit
                    : TransactionType.Credit,
                Description = item.TryGetProperty("TransactionName", out var tn) ? tn.GetString() ?? "" : "",
                TransactionDate = item.TryGetProperty("TransactionDate", out var td) &&
                                  DateTime.TryParse(td.GetString(), out var dt) ? dt : DateTime.MinValue,
                BalanceAfterTransaction = item.TryGetProperty("Balance", out var bal) ? ParseDecimal(bal) : 0
            });
        }

        return transactions;
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

    public async Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId)
    {
        // Sandbox'ta tüm hesaplar tek API kullanıcısına ait olduğundan sahiplik hep doğru.
        // Gerçek çok kullanıcılı senaryoda kullanıcı-hesap eşlemesi gerekir.
        return await Task.FromResult(true);
    }

    public async Task<TransactionDto?> AddTransactionAsync(
        int accountId, int userId, CreateTransactionDto dto)
    {
        // VakıfBank sandbox'ı işlem ekleme sunmuyor; bu özellik MSSQL kaynağında çalışır.
        return await Task.FromResult<TransactionDto?>(null);
    }
}