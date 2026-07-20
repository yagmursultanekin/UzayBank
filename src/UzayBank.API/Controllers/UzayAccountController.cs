using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using System.Security.Claims;

namespace UzayBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UzayAccountController : ControllerBase
{
    private readonly IUzayAccountService _uzayAccountService;

    public UzayAccountController(IUzayAccountService uzayAccountService)
    {
        _uzayAccountService = uzayAccountService;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : int.Parse(claim);
    }

    /// <summary>Kullanıcının UzayBank hesapları.</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAccounts()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var accounts = await _uzayAccountService.GetMyAccountsAsync(userId.Value);
        return Ok(accounts);
    }

    /// <summary>Yeni UzayBank hesabı açar.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount(CreateUzayAccountDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var account = await _uzayAccountService.CreateAccountAsync(userId.Value, dto);
        return Ok(account);
    }

    /// <summary>Hesabın işlem geçmişi.</summary>
    [HttpGet("{accountId:int}/transactions")]
    public async Task<IActionResult> GetTransactions(int accountId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var transactions = await _uzayAccountService.GetTransactionsAsync(accountId, userId.Value);
        return Ok(transactions);
    }

    /// <summary>Hesaplar arası transfer.</summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _uzayAccountService.TransferAsync(userId.Value, dto);

        // Servis hata KODU dönüyor, frontend çeviriyor (i18n yapımıza uygun)
        if (!result.Success)
            return BadRequest(new { code = result.ErrorCode });

        return Ok(result);
    }

    /// <summary>Hesaba para yatırır.</summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(DepositWithdrawDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _uzayAccountService.DepositAsync(userId.Value, dto);

        if (!result.Success)
            return BadRequest(new { code = result.ErrorCode });

        return Ok(result);
    }

    /// <summary>Hesaptan para çeker.</summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(DepositWithdrawDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _uzayAccountService.WithdrawAsync(userId.Value, dto);

        if (!result.Success)
            return BadRequest(new { code = result.ErrorCode });

        return Ok(result);
    }
}