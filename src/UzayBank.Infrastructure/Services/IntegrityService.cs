using Microsoft.EntityFrameworkCore;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Interfaces;
using UzayBank.Domain.Services;
using UzayBank.Infrastructure.Persistence;

namespace UzayBank.Infrastructure.Services;

public class IntegrityService : IIntegrityService
{
    private readonly UzayBankDbContext _context;
    private readonly ITransactionHasher _hasher;

    public IntegrityService(UzayBankDbContext context, ITransactionHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<AccountIntegrityDto?> VerifyAccountAsync(int accountId, int userId)
    {
        // Sahiplik kontrolü — başkasının hesabı doğrulanamaz.
        var owns = await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!owns)
            return null;

        // İşlemleri zincir sırasına göre çekiyoruz.
        //
        // Id'ye göre ARTAN sıralama kritik: zinciri baştan sona takip etmemiz
        // gerekiyor. Tarihe göre sıralamak güvenilir değil, çünkü bir transferin
        // iki kaydı aynı TransactionDate değerini taşıyor.
        var transactions = await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderBy(t => t.Id)
            .ToListAsync();

        var result = new AccountIntegrityDto
        {
            AccountId = accountId,
            TotalTransactions = transactions.Count
        };

        // Zincirdeki bir önceki halkanın hash'i.
        // İlk kayıt için GENESIS bekliyoruz.
        string expectedPreviousHash = TransactionHasher.GenesisHash;

        foreach (var transaction in transactions)
        {
            var item = new TransactionIntegrityDto
            {
                TransactionId = transaction.Id,
                TransactionRef = transaction.TransactionRef,
                TransactionDate = transaction.TransactionDate,
                Amount = transaction.Amount,
                Description = transaction.Description,
                StoredHash = transaction.TxHash
            };

            // Hash alanları eklenmeden önce oluşmuş kayıtlar.
            // Bunlar kurcalanmış değil, sadece kapsam dışı — zincir
            // bu kayıtlardan sonra başlıyor.
            if (string.IsNullOrWhiteSpace(transaction.TxHash))
            {
                item.IsUnhashed = true;
                result.UnhashedCount++;
                result.Transactions.Add(item);
                continue;
            }

            // KONTROL 1: Kaydın kendi içeriği bozulmuş mu?
            // Alanlardan hash'i yeniden hesaplayıp saklanan değerle karşılaştırıyoruz.
            // Herhangi bir alan (tutar, tarih, açıklama...) değiştiyse tutmaz.
            var computed = _hasher.ComputeHash(
                transaction,
                transaction.PreviousTxHash ?? TransactionHasher.GenesisHash);

            item.ComputedHash = computed;
            item.IsHashValid = string.Equals(
                computed, transaction.TxHash, StringComparison.Ordinal);

            // KONTROL 2: Zincir bağlantısı sağlam mı?
            // Bu kaydın PreviousTxHash'i, bir önceki kaydın TxHash'i olmalı.
            // Tutmazsa araya kayıt eklenmiş, silinmiş veya sıra bozulmuş demektir.
            var actualPrevious = transaction.PreviousTxHash ?? TransactionHasher.GenesisHash;
            item.IsChainValid = string.Equals(
                actualPrevious, expectedPreviousHash, StringComparison.Ordinal);

            if (!item.IsHashValid || !item.IsChainValid)
                result.InvalidCount++;

            // Sonraki kayıt için beklenen değer: bu kaydın saklanan hash'i.
            //
            // Hesaplanan değil, SAKLANAN hash'i kullanıyoruz. Sebep: bu kayıt
            // kurcalanmışsa bile zincirin geri kalanını doğru değerlendirmek
            // istiyoruz — tek bir bozuk kayıt sonraki hepsini "bozuk" göstermesin.
            expectedPreviousHash = transaction.TxHash;

            result.Transactions.Add(item);
        }

        // Hash'i olmayan kayıtlar sayılmıyor; yalnızca kontrol edilebilenler
        // arasında sorun varsa zincir geçersiz.
        result.IsValid = result.InvalidCount == 0;

        return result;
    }
}