using Microsoft.Extensions.Configuration;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Enums;
using UzayBank.Domain.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UzayBank.Infrastructure.ExternalServices;

public class VakifBankAccountService : IAccountService
{
    private readonly HttpClient _httpClient;
    private readonly VakifBankAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IUserAccountRepository _userAccounts;

    // VakıfBank'tan gelen HAM hesap listesi.
    // Sandbox tek kurumsal kimlik verdiği için bu liste tüm kullanıcılar için aynıdır.
    private static List<AccountDto>? _cachedAllAccounts;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly object _cacheLock = new();

    public VakifBankAccountService(
        HttpClient httpClient,
        VakifBankAuthService authService,
        IConfiguration configuration,
        IUserAccountRepository userAccounts)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configuration = configuration;
        _userAccounts = userAccounts;
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

        // EnsureSuccessStatusCode() hata gövdesini kaybediyordu.
        // Kendi exception'ımız ham cevabı taşır — böylece ACBH000202 (hareket yok)
        // ile gerçek hatalar (rıza geçersiz, yetki vb.) ayırt edilebilir.
        if (!response.IsSuccessStatusCode)
            throw new VakifBankApiException(response.StatusCode, content);

        return content;
    }

    /// <summary>
    /// VakıfBank'tan TÜM hesapları çeker. Kullanıcı ayrımı yapmaz.
    /// Sandbox tek kurumsal kimlik verdiği için gelen liste herkes için aynıdır.
    /// </summary>
    private async Task<List<AccountDto>> GetAllAccountsFromApiAsync()
    {
        lock (_cacheLock)
        {
            if (_cachedAllAccounts != null && DateTime.UtcNow < _cacheExpiry)
                return _cachedAllAccounts;
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
                Id = id++, // Liste sırasından üretilen kimlik — gerçek kimlik AccountNumber
                AccountNumber = item.TryGetProperty("AccountNumber", out var an) ? an.GetString() ?? "" : "",
                IBAN = item.TryGetProperty("IBAN", out var ib) ? ib.GetString() ?? "" : "",
                Currency = item.TryGetProperty("CurrencyCode", out var cc) ? cc.GetString() ?? "" : "",
                Balance = item.TryGetProperty("Balance", out var b) ? ParseDecimal(b) : 0,
                AccountHolderName = ""
            });
        }

        lock (_cacheLock)
        {
            _cachedAllAccounts = accounts;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(2);
        }

        return accounts;
    }

    /// <summary>
    /// GEÇİCİ: Eşleme stratejisi belirlenmediği için kullanıcı bazlı filtreleme devre dışı.
    /// UserAccounts tablosu ve repository hazır — karar verilince yorum satırları açılacak.
    /// </summary>

    public async Task<List<AccountDto>> GetAccountsByUserIdAsync(int userId)
    {
        var allAccounts = await GetAllAccountsFromApiAsync();

        // Kullanıcıya atanmış hesap numaralarını al, sadece onları döndür
        var linkedNumbers = await _userAccounts.GetAccountNumbersByUserIdAsync(userId);
        return allAccounts.Where(a => linkedNumbers.Contains(a.AccountNumber)).ToList();
    }
    /// <summary>
    /// GEÇİCİ: Sandbox tek kurumsal kimlik verdiği ve eşleme stratejisi
    /// belirlenmediği için sahiplik kontrolü şu an devre dışı.
    /// </summary>
    public async Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId)
    {
        var allAccounts = await GetAllAccountsFromApiAsync();
        var account = allAccounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
            return false;

        return await _userAccounts.IsLinkedAsync(userId, account.AccountNumber);
    }

    public async Task<List<TransactionDto>> GetTransactionsByAccountIdAsync(
        int accountId, DateTime startDate, DateTime endDate)
    {
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

    public async Task<TransactionDto?> AddTransactionAsync(
        int accountId, int userId, CreateTransactionDto dto)
    {
        // VakıfBank sandbox'ı işlem ekleme sunmuyor; bu özellik MSSQL kaynağında çalışır.
        return await Task.FromResult<TransactionDto?>(null);
    }

    public async Task<List<TransactionDto>> GetAllTransactionsByUserIdAsync(
        int userId, DateTime startDate, DateTime endDate)
    {
        var accounts = await GetAccountsByUserIdAsync(userId);

        // Hesapları PARALEL çek — sırayla çekseydik 6 hesap × ~350ms = 2+ saniye.
        // Paralel çekince toplam süre en yavaş isteğin süresi kadar (~350ms).
        var tasks = accounts.Select(a =>
            GetTransactionsByAccountIdAsync(a.Id, startDate, endDate));

        var results = await Task.WhenAll(tasks);

        // Tüm hesapların işlemlerini tek listede birleştir, tarihe göre sırala (yeniden eskiye)
        return results
            .SelectMany(list => list)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId)
    {
        var accounts = await GetAllAccountsFromApiAsync();
        return accounts.FirstOrDefault(a => a.Id == accountId);
    }

    public async Task<List<AccountDto>> GetAllAccountsForAdminAsync()
    {
        return await GetAllAccountsFromApiAsync();
    }
}