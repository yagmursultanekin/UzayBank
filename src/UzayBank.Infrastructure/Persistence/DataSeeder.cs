using UzayBank.Domain.Entities;
using UzayBank.Domain.Enums;

namespace UzayBank.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(UzayBankDbContext context)
    {
        // Zaten veri varsa tekrar ekleme
        if (context.Users.Any())
            return;

        // Test kullanıcısı oluştur
        var user = new User
        {
            FullName = "Yağmur Sultan",
            Email = "yagmur@uzaybank.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Hesaplar oluştur
        var vadesizHesap = new Account
        {
            AccountNumber = "0015800000001",
            IBAN = "TR330006200015800000001",
            Currency = "TRY",
            Balance = 25750.50m,
            AccountHolderName = "Yağmur Sultan",
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        var dovizHesap = new Account
        {
            AccountNumber = "0015800000002",
            IBAN = "TR330006200015800000002",
            Currency = "USD",
            Balance = 1250.00m,
            AccountHolderName = "Yağmur Sultan",
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        Console.WriteLine($"User Id: {user.Id}");
        context.Accounts.AddRange(vadesizHesap, dovizHesap);
        await context.SaveChangesAsync();

        // İşlemler oluştur
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                AccountId = vadesizHesap.Id,
                Amount = 5000,
                Type = TransactionType.Credit,
                Description = "Maaş yatışı",
                TransactionDate = DateTime.UtcNow.AddDays(-20),
                BalanceAfterTransaction = 25000
            },
            new Transaction
            {
                AccountId = vadesizHesap.Id,
                Amount = 1200,
                Type = TransactionType.Debit,
                Description = "Market alışverişi",
                TransactionDate = DateTime.UtcNow.AddDays(-15),
                BalanceAfterTransaction = 23800
            },
            new Transaction
            {
                AccountId = vadesizHesap.Id,
                Amount = 3500,
                Type = TransactionType.Credit,
                Description = "Freelance ödeme",
                TransactionDate = DateTime.UtcNow.AddDays(-10),
                BalanceAfterTransaction = 27300
            },
            new Transaction
            {
                AccountId = vadesizHesap.Id,
                Amount = 850,
                Type = TransactionType.Debit,
                Description = "Fatura ödemesi",
                TransactionDate = DateTime.UtcNow.AddDays(-5),
                BalanceAfterTransaction = 26450
            },
            new Transaction
            {
                AccountId = vadesizHesap.Id,
                Amount = 699.50m,
                Type = TransactionType.Debit,
                Description = "Online alışveriş",
                TransactionDate = DateTime.UtcNow.AddDays(-2),
                BalanceAfterTransaction = 25750.50m
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }
}