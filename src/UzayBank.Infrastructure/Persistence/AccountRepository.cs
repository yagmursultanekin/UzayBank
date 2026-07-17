using Microsoft.EntityFrameworkCore;
using UzayBank.Domain.Entities;
using UzayBank.Domain.Interfaces;

namespace UzayBank.Infrastructure.Persistence;

public class AccountRepository : IAccountRepository //bu sınıf sözleşmeyi implenemte ediyor. interfaceteki 3 metodun gerçek kodunu burada yazıyoruz
{
    private readonly UzayBankDbContext _context;

    public AccountRepository(UzayBankDbContext context)
    {
        _context = context;
    }

    public async Task<List<Account>> GetByUserIdAsync(int userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<Account?> GetByIdAsync(int accountId)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<List<Transaction>> GetTransactionsAsync(int accountId,DateTime startDate, DateTime endDate)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId
            && t.TransactionDate >= startDate
            && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<bool> IsAccountOwnedByUserAsync(int accountId, int userId)
    {
        return await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);
    }
    public async Task AddTransactionAsync(Transaction transaction, Account account)
    {
        _context.Transactions.Add(transaction);
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }
    public async Task<List<Account>> GetAllAsync()
    {
        return await _context.Accounts.ToListAsync();
    }
}
