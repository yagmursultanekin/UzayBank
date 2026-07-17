using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Interfaces;
using UzayBank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UzayBank.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IAccountService _accountService;
    private readonly IUserAccountRepository _userAccounts;
    private readonly UzayBankDbContext _context;

    public AdminService(
        IAccountService accountService,
        IUserAccountRepository userAccounts,
        UzayBankDbContext context)
    {
        _accountService = accountService;
        _userAccounts = userAccounts;
        _context = context;
    }

    public async Task<List<AccountAssignmentDto>> GetAllAssignmentsAsync()
    {
        // VakıfBank'tan tüm hesapları al (userId=0 → filtresiz ham liste değil;
        // burada admin özel bir metoda ihtiyaç duyuyor — aşağıdaki nota bak)
        var allAccounts = await _accountService.GetAllAccountsForAdminAsync();

        // Mevcut atamaları tek sorguda çek (hesap no → kullanıcı)
        var links = await _context.UserAccounts
            .Include(ua => ua.User)
            .ToListAsync();

        var linkMap = links.ToDictionary(l => l.AccountNumber, l => l.User);

        return allAccounts.Select(a =>
        {
            linkMap.TryGetValue(a.AccountNumber, out var user);
            return new AccountAssignmentDto
            {
                AccountNumber = a.AccountNumber,
                IBAN = a.IBAN,
                Currency = a.Currency,
                AssignedUserId = user?.Id,
                AssignedUserEmail = user?.Email
            };
        }).ToList();
    }

    public async Task<bool> AssignAccountAsync(int userId, string accountNumber)
    {
        // Tek kişiye kuralı: hesap zaten atanmışsa reddet
        if (await _userAccounts.IsAccountTakenAsync(accountNumber))
            return false;

        // Hesap bilgilerini VakıfBank listesinden bul (IBAN, currency için)
        var allAccounts = await _accountService.GetAllAccountsForAdminAsync();
        var account = allAccounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
        if (account == null)
            return false;

        await _userAccounts.LinkAsync(userId, accountNumber, account.IBAN, account.Currency);
        return true;
    }

    public async Task UnassignAccountAsync(string accountNumber)
    {
        await _userAccounts.UnlinkAsync(accountNumber);
    }
}