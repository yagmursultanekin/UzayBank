using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexBank.Application.DTOs;
using NexBank.Application.Interfaces;
using System.Security.Claims;

namespace NexBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
//localhost:4000/api/Account/my-accounts

public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }

    [HttpGet("my-accounts")]
    public async Task<IActionResult> GetMyAccounts()
    {
        var accounts = await _accountService.GetAccountsByUserIdAsync(GetUserId());
        return Ok(accounts);
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetAccountById(int accountId)
    {
        var isOwner = await _accountService.IsAccountOwnedByUserAsync(accountId, GetUserId());
        if (!isOwner)
            return Forbid();

        var account = await _accountService.GetAccountByIdAsync(accountId);
        if (account == null)
            return NotFound();

        return Ok(account);
    }

    [HttpGet("{accountId}/transactions")]
    public async Task<IActionResult> GetTransactions(int accountId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var isOwner = await _accountService.IsAccountOwnedByUserAsync(accountId, GetUserId());
        if (!isOwner)
            return Forbid();

        var transactions = await _accountService.GetTransactionsByAccountIdAsync(accountId, startDate, endDate);
        return Ok(transactions);
    }
    [HttpPost("{accountId}/transactions")]
    public async Task<IActionResult> AddTransaction(int accountId, CreateTransactionDto dto)
    {
        var result = await _accountService.AddTransactionAsync(accountId, GetUserId(), dto);
        if (result == null)
            return Forbid();
        return Ok(result);
    }
}