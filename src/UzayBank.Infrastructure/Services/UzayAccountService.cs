using Microsoft.EntityFrameworkCore;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Entities;
using UzayBank.Domain.Enums;
using UzayBank.Infrastructure.Persistence;

namespace UzayBank.Infrastructure.Services;

public class UzayAccountService : IUzayAccountService
{
    private readonly UzayBankDbContext _context;

    public UzayAccountService(UzayBankDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountDto>> GetMyAccountsAsync(int userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId && a.Source == AccountSource.UzayBank)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                IBAN = a.IBAN,
                Currency = a.Currency,
                Balance = a.Balance,
                AccountHolderName = a.AccountHolderName
            })
            .ToListAsync();
    }

    public async Task<AccountDto> CreateAccountAsync(int userId, CreateUzayAccountDto dto)
    {
        var user = await _context.Users.FirstAsync(u => u.Id == userId);

        var accountNumber = await GenerateUniqueAccountNumberAsync();

        var account = new Account
        {
            UserId = userId,
            AccountNumber = accountNumber,
            IBAN = GenerateIban(accountNumber),
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TL" : dto.Currency,
            Balance = 0,
            AccountHolderName = user.FullName,
            CreatedAt = DateTime.UtcNow,
            Source = AccountSource.UzayBank
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            IBAN = account.IBAN,
            Currency = account.Currency,
            Balance = account.Balance,
            AccountHolderName = account.AccountHolderName
        };
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(int accountId, int userId)
    {
        // Sahiplik kontrolü — başkasının hesabının hareketleri görülemez
        var owns = await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!owns)
            return new List<TransactionDto>();

        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionDto
            {
                ID = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Description = t.Description,
                TransactionDate = t.TransactionDate,
                BalanceAfterTransaction = t.BalanceAfterTransaction
            })
            .ToListAsync();
    }

    public async Task<TransferResultDto> TransferAsync(int userId, TransferDto dto)
    {
        if (dto.Amount <= 0)
            return Fail("INVALID_AMOUNT");

        // Gönderen hesap — sahiplik kontrolü burada
        var from = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.FromAccountId && a.UserId == userId);

        if (from == null)
            return Fail("SENDER_ACCOUNT_NOT_FOUND");

        var toIban = dto.ToIban?.Replace(" ", "").Trim().ToUpperInvariant() ?? "";

        var to = await _context.Accounts
            .FirstOrDefaultAsync(a => a.IBAN == toIban);

        if (to == null)
            return Fail("RECIPIENT_NOT_FOUND");

        if (to.Id == from.Id)
            return Fail("SAME_ACCOUNT");

        if (from.Balance < dto.Amount)
            return Fail("INSUFFICIENT_FUNDS");

        // Para iki hesap arasında yer değiştiriyor — ya hepsi olsun ya hiçbiri.
        // Yarısı işlenirse para buharlaşır veya çoğalır.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            from.Balance -= dto.Amount;
            to.Balance += dto.Amount;

            var now = DateTime.UtcNow;
            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? "Transfer"
                : dto.Description;

            _context.Transactions.Add(new Transaction
            {
                AccountId = from.Id,
                Amount = -dto.Amount,
                Type = TransactionType.Debit,
                Description = $"{description} → {to.AccountHolderName}",
                TransactionDate = now,
                BalanceAfterTransaction = from.Balance
            });

            _context.Transactions.Add(new Transaction
            {
                AccountId = to.Id,
                Amount = dto.Amount,
                Type = TransactionType.Credit,
                Description = $"{description} ← {from.AccountHolderName}",
                TransactionDate = now,
                BalanceAfterTransaction = to.Balance
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new TransferResultDto
            {
                Success = true,
                NewBalance = from.Balance
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            return Fail("TRANSFER_FAILED");
        }
    }

    public async Task<TransferResultDto> DepositAsync(int userId, DepositWithdrawDto dto)
    {
        if (dto.Amount <= 0)
            return Fail("INVALID_AMOUNT");

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);

        if (account == null)
            return Fail("ACCOUNT_NOT_FOUND");

        account.Balance += dto.Amount;

        _context.Transactions.Add(new Transaction
        {
            AccountId = account.Id,
            Amount = dto.Amount,
            Type = TransactionType.Credit,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? "Para Yatırma" : dto.Description,
            TransactionDate = DateTime.UtcNow,
            BalanceAfterTransaction = account.Balance
        });

        await _context.SaveChangesAsync();

        return new TransferResultDto { Success = true, NewBalance = account.Balance };
    }

    public async Task<TransferResultDto> WithdrawAsync(int userId, DepositWithdrawDto dto)
    {
        if (dto.Amount <= 0)
            return Fail("INVALID_AMOUNT");

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);

        if (account == null)
            return Fail("ACCOUNT_NOT_FOUND");

        if (account.Balance < dto.Amount)
            return Fail("INSUFFICIENT_FUNDS");

        account.Balance -= dto.Amount;

        _context.Transactions.Add(new Transaction
        {
            AccountId = account.Id,
            Amount = -dto.Amount,
            Type = TransactionType.Debit,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? "Para Çekme" : dto.Description,
            TransactionDate = DateTime.UtcNow,
            BalanceAfterTransaction = account.Balance
        });

        await _context.SaveChangesAsync();

        return new TransferResultDto { Success = true, NewBalance = account.Balance };
    }

    // --- Yardımcılar ---

    private static TransferResultDto Fail(string code) =>
        new() { Success = false, ErrorCode = code };

    /// <summary>
    /// 16 haneli benzersiz hesap numarası üretir.
    /// Çakışma ihtimaline karşı veritabanında kontrol edilir.
    /// </summary>
    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        var random = new Random();
        string accountNumber;

        do
        {
            accountNumber = "9" + string.Concat(
                Enumerable.Range(0, 15).Select(_ => random.Next(0, 10).ToString()));
        }
        while (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

        return accountNumber;
    }

    /// <summary>
    /// Hesap numarasından IBAN üretir.
    /// UzayBank kurgusal bir banka olduğu için banka kodu olarak 99 kullanılıyor;
    /// format gerçek TR IBAN'ına benzetilmiştir (TR + 2 kontrol + 5 banka + 16 hesap).
    /// </summary>
    private static string GenerateIban(string accountNumber)
    {
        return $"TR99{accountNumber[..1]}0099{accountNumber}";
    }
}