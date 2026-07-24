using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UzayBank.Application.Interfaces;
using UzayBank.Infrastructure.Persistence;

namespace UzayBank.Infrastructure.Blockchain;

/// <summary>
/// Sabitleme kuyruğunu düzenli aralıklarla boşaltır.
///
/// NEDEN AYRI BİR SERVİS:
/// Blockchain'e yazma yavaş ve dış bir sisteme bağımlı. Para transferi
/// akışının içinde olsaydı kullanıcı beklerdi ve blockchain çöktüğünde
/// bankacılık işlemi de başarısız olurdu. Burada ayırıyoruz: işlem anında
/// tamamlanır, sabitleme arka planda kendi hızında ilerler.
/// </summary>
public class AnchorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnchorBackgroundService> _logger;

    /// <summary>Kuyruk kontrol aralığı.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Bir kayıt en fazla kaç kez denenir.
    ///
    /// Blockchain kalıcı olarak erişilemez durumdaysa sonsuza kadar denemek
    /// hem log'u doldurur hem kaynak harcar. Bu eşikten sonra kayıt es geçilir
    /// ve elle müdahale gerekir.
    /// </summary>
    private const int MaxAttempts = 5;

    public AnchorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AnchorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sabitleme servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Döngünün kendisi asla çökmemeli — bir turda hata olsa bile
                // servis çalışmaya devam etmeli.
                _logger.LogError(ex, "Sabitleme turunda beklenmeyen hata.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        // BackgroundService singleton'dır, DbContext ise scoped.
        // Singleton bir servise scoped bağımlılık enjekte edilemez —
        // bu yüzden her turda kendi scope'umuzu oluşturuyoruz.
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<UzayBankDbContext>();
        var blockchain = scope.ServiceProvider.GetRequiredService<IBlockchainAnchorService>();

        var pending = await context.AnchorQueue
            .Where(q => !q.IsProcessed && q.AttemptCount < MaxAttempts)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        // Hesap bazında grupluyoruz.
        //
        // Bir hesap için beş bekleyen kayıt varsa beş kez yazmaya gerek yok:
        // zincirin son hash'i zaten öncekilerin hepsini kapsıyor. Tek yazma
        // hepsini karşılar.
        var groups = pending.GroupBy(q => q.AccountId);

        foreach (var group in groups)
        {
            var accountId = group.Key;
            var items = group.ToList();

            try
            {
                // Hesabın zincirindeki son hash.
                var lastHash = await context.Transactions
                    .Where(t => t.AccountId == accountId && t.TxHash != null)
                    .OrderByDescending(t => t.Id)
                    .Select(t => t.TxHash)
                    .FirstOrDefaultAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(lastHash))
                {
                    // Hash'i olan hiç işlem yok — sabitlenecek bir şey de yok.
                    // Kayıtları işlenmiş sayıp geçiyoruz, aksi hâlde sonsuza
                    // kadar kuyrukta kalırlar.
                    MarkProcessed(items, txHash: null);
                    continue;
                }

                var blockchainTxHash = await blockchain.AnchorAsync(accountId, lastHash);

                MarkProcessed(items, blockchainTxHash);

                _logger.LogInformation(
                    "Hesap {AccountId} sabitlendi. {Count} kuyruk kaydı kapatıldı. İşlem: {TxHash}",
                    accountId, items.Count, blockchainTxHash);
            }
            catch (Exception ex)
            {
                // Bu hesap için sabitleme başarısız oldu. Kayıtları işlenmiş
                // İŞARETLEMİYORUZ — bir sonraki turda tekrar denenecekler.
                foreach (var item in items)
                {
                    item.AttemptCount++;
                    item.LastError = Truncate(ex.Message, 1000);
                }

                _logger.LogWarning(ex,
                    "Hesap {AccountId} sabitlenemedi. Deneme: {Attempt}/{Max}",
                    accountId, items[0].AttemptCount, MaxAttempts);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void MarkProcessed(List<Domain.Entities.AnchorQueueItem> items, string? txHash)
    {
        var now = DateTime.UtcNow;

        foreach (var item in items)
        {
            item.IsProcessed = true;
            item.ProcessedAt = now;
            item.BlockchainTxHash = txHash;
            item.LastError = null;
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}