using Microsoft.EntityFrameworkCore;
using UzayBank.Domain.Entities;
using UzayBank.Domain.Interfaces;

namespace UzayBank.Infrastructure.Persistence;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly UzayBankDbContext _context;

    public UserAccountRepository(UzayBankDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetAccountNumbersByUserIdAsync(int userId)
    {
        return await _context.UserAccounts
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AccountNumber)
            .ToListAsync();
    }

    public async Task<bool> IsLinkedAsync(int userId, string accountNumber)
    {
        return await _context.UserAccounts
            .AnyAsync(ua => ua.UserId == userId && ua.AccountNumber == accountNumber);
    }

    public async Task LinkAsync(int userId, string accountNumber, string iban, string currency)
    {
        // Zaten bağlıysa tekrar ekleme — unique index zaten engellerdi,
        // ama exception fırlatmaktansa sessizce geçmek daha temiz.
        if (await IsLinkedAsync(userId, accountNumber))
            return;

        _context.UserAccounts.Add(new UserAccount
        {
            UserId = userId,
            AccountNumber = accountNumber,
            IBAN = iban,
            Currency = currency
        });

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsAccountTakenAsync(string accountNumber)
    {
        return await _context.UserAccounts
            .AnyAsync(ua => ua.AccountNumber == accountNumber);
    }

    public async Task UnlinkAsync(string accountNumber)
    {
        var link = await _context.UserAccounts
            .FirstOrDefaultAsync(ua => ua.AccountNumber == accountNumber);

        if (link != null)
        {
            _context.UserAccounts.Remove(link);
            await _context.SaveChangesAsync();
        }
    }
}
