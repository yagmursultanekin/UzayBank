using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using System.Security.Claims;

namespace UzayBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IUzayAccountService _uzayAccountService;

    public AccountController(
        IAccountService accountService,
        IUzayAccountService uzayAccountService)
    {
        _accountService = accountService;
        _uzayAccountService = uzayAccountService;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : int.Parse(claim);
    }

    [HttpGet("my-accounts")]
    public async Task<IActionResult> GetMyAccounts()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var accounts = await _accountService.GetAccountsByUserIdAsync(userId.Value);
        return Ok(accounts);
    }

    [HttpGet("{accountId:int}")]
    public async Task<IActionResult> GetAccountById(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var isOwner = await _accountService.IsAccountOwnedByUserAsync(accountId, userId.Value);
        if (!isOwner)
            return Forbid();

        var account = await _accountService.GetAccountByIdAsync(accountId);
        if (account == null)
            return NotFound();

        return Ok(account);
    }

    [HttpGet("all-transactions")]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        // Hibrit model: analiz her iki hesap kaynağını da kapsamalı.
        //   1) Banka hesapları  → IAccountService (VakıfBank API veya MSSQL)
        //   2) UzayBank hesapları → IUzayAccountService (her zaman MSSQL)
        //
        // Daha önce yalnızca birincisi çekildiği için MSSQL'deki işlemler
        // (seed data ve kullanıcının kendi UzayBank hareketleri) analize girmiyordu.
        //
        // Birleştirme şimdilik controller'da; katman refactor'ünde bu mantık
        // Application katmanına taşınacak.

        var bankTransactions = await _accountService.GetAllTransactionsByUserIdAsync(
            userId.Value, startDate, endDate);

        var uzayAccounts = await _uzayAccountService.GetMyAccountsAsync(userId.Value);

        var uzayTransactions = new List<TransactionDto>();
        foreach (var account in uzayAccounts)
        {
            var txs = await _uzayAccountService.GetTransactionsAsync(account.Id, userId.Value);

            // Servis artık yetkisiz erişimde null dönüyor. Burada null beklenmez
            // (hesapları zaten bu kullanıcı için çektik) ama derleyici uyarısını
            // susturmak ve olası bir mantık hatasında çökmemek için kontrol ediyoruz.
            if (txs == null)
                continue;

            uzayTransactions.AddRange(
                txs.Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate.AddDays(1)));
        }

        var combined = bankTransactions
            .Concat(uzayTransactions)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        return Ok(combined);
    }

    [HttpGet("{accountId}/transactions")]
    public async Task<IActionResult> GetTransactions(int accountId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var isOwner = await _accountService.IsAccountOwnedByUserAsync(accountId, userId.Value);
        if (!isOwner)
            return Forbid();

        var transactions = await _accountService.GetTransactionsByAccountIdAsync(accountId, startDate, endDate);
        return Ok(transactions);
    }

    [HttpPost("{accountId}/transactions")]
    public async Task<IActionResult> AddTransaction(int accountId, CreateTransactionDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _accountService.AddTransactionAsync(accountId, userId.Value, dto);
        if (result == null)
            return Forbid();

        return Ok(result);
    }
}