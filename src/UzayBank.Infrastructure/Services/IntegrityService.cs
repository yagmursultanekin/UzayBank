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
    private readonly IBlockchainAnchorService _blockchain;

    public IntegrityService(
        UzayBankDbContext context,
        ITransactionHasher hasher,
        IBlockchainAnchorService blockchain)
    {
        _context = context;
        _hasher = hasher;
        _blockchain = blockchain;
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
            var computed = _hasher.ComputeHash(
                transaction,
                transaction.PreviousTxHash ?? TransactionHasher.GenesisHash);

            item.ComputedHash = computed;
            item.IsHashValid = string.Equals(
                computed, transaction.TxHash, StringComparison.Ordinal);

            // KONTROL 2: Zincir bağlantısı sağlam mı?
            var actualPrevious = transaction.PreviousTxHash ?? TransactionHasher.GenesisHash;
            item.IsChainValid = string.Equals(
                actualPrevious, expectedPreviousHash, StringComparison.Ordinal);

            if (!item.IsHashValid || !item.IsChainValid)
                result.InvalidCount++;

            // Sonraki kayıt için beklenen değer: bu kaydın SAKLANAN hash'i.
            // Hesaplanan değil — tek bir bozuk kayıt sonrakilerin hepsini
            // bozuk göstermesin diye.
            expectedPreviousHash = transaction.TxHash;

            result.Transactions.Add(item);
        }

        result.IsValid = result.InvalidCount == 0;

        // Veritabanı zincirinin son halkası. Blockchain'e sabitlenmesi
        // gereken değer bu.
        result.CurrentChainHash = transactions
            .LastOrDefault(t => !string.IsNullOrWhiteSpace(t.TxHash))?.TxHash;

        // KONTROL 3: Blockchain doğrulaması.
        //
        // Buraya kadar yaptığımız kontroller yalnızca veritabanının KENDİ İÇİNDE
        // tutarlı olduğunu gösterir. Veritabanına tam erişimi olan biri zinciri
        // baştan yeniden hesaplayarak bu kontrollerin hepsini geçebilir.
        //
        // Blockchain'deki kayıt veritabanının dışında ve değiştirilemez olduğu
        // için, asıl koruma bu karşılaştırmadan geliyor.
        await ApplyAnchorStatusAsync(result, transactions);

        return result;
    }

    /// <summary>
    /// Veritabanı zincirini blockchain'e sabitlenmiş kayıtla karşılaştırır.
    /// </summary>
    private async Task ApplyAnchorStatusAsync(
        AccountIntegrityDto result,
        List<Domain.Entities.Transaction> transactions)
    {
        var anchor = await _blockchain.GetAnchorAsync(result.AccountId);

        if (anchor == null)
        {
            // Bu hesap için henüz hiç sabitleme yapılmamış.
            result.AnchorStatus = "NotAnchored";
            result.IsAnchorValid = false;
            return;
        }

        result.AnchoredHash = anchor.Hash;
        result.AnchoredAt = anchor.AnchoredAt;

        // En iyi durum: sabitlenen hash zincirin şu anki son halkası.
        if (string.Equals(anchor.Hash, result.CurrentChainHash, StringComparison.OrdinalIgnoreCase))
        {
            result.AnchorStatus = "Matched";
            result.IsAnchorValid = true;
            return;
        }

        // Sabitlenen hash zincirin sonu değil — ama veritabanında bir yerde
        // geçiyor mu? Geçiyorsa, o kayıt hâlâ yerinde demektir; sadece
        // sonrasında yeni işlemler olmuş ve henüz sabitlenmemiş.
        var existsInChain = transactions.Any(t =>
            string.Equals(t.TxHash, anchor.Hash, StringComparison.OrdinalIgnoreCase));

        if (existsInChain)
        {
            result.AnchorStatus = "Outdated";

            // Bu bir kurcalama belirtisi DEĞİL. Sabitlenen an itibarıyla
            // veritabanı doğruydu, sonrasında yeni işlemler eklendi.
            result.IsAnchorValid = true;
            return;
        }

        // Sabitlenen hash veritabanının HİÇBİR kaydında yok.
        // Bu ciddi: o kayıt ya silinmiş ya da içeriği değiştirilmiş.
        result.AnchorStatus = "Mismatch";
        result.IsAnchorValid = false;
    }
}