using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexBank.Application.DTOs;
using NexBank.Application.Interfaces;
using System.Security.Claims;

namespace NexBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
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

    [HttpGet("{accountId:int}/transactions")]
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