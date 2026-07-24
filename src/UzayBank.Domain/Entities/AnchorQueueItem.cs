namespace UzayBank.Domain.Entities;

/// <summary>
/// Blockchain'e sabitlenmeyi bekleyen hesapları tutan kuyruk kaydı.
///
/// NEDEN KUYRUK:
/// Blockchain'e yazma işlemi yavaş (blok onayı bekler) ve dış bir sisteme
/// bağımlıdır. Bunu para transferi akışının içine koyarsak, kullanıcı
/// bekler ve blockchain çöktüğünde bankacılık işlemi de başarısız olur.
///
/// Bunun yerine işlem sırasında yalnızca "bu hesap sabitlenmeli" notu
/// bırakıyoruz. Not, işlemin AYNI veritabanı transaction'ında yazıldığı
/// için ya ikisi birden olur ya hiçbiri — "işlem kaydedildi ama not
/// kayboldu" durumu oluşamaz.
///
/// Bu desen Outbox Pattern olarak bilinir.
/// </summary>
public class AnchorQueueItem
{
    public int Id { get; set; }

    /// <summary>Sabitlenmesi gereken hesap.</summary>
    public int AccountId { get; set; }

    /// <summary>Notun oluşturulduğu an.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// İşlendi mi? Arka plan servisi başarıyla yazdıktan sonra true olur.
    ///
    /// Kayıtları silmek yerine işaretliyoruz: geçmişte hangi hesabın ne zaman
    /// sabitlendiği bilgisi denetim açısından değerli.
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>Blockchain'e yazıldığı an. İşlenmemişse null.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Blockchain'deki işlem kimliği (transaction hash).
    /// Sabitlemenin kanıtı — ileride bu referansla zincirdeki kayda ulaşılabilir.
    /// </summary>
    public string? BlockchainTxHash { get; set; }

    /// <summary>
    /// Kaç kez denendi.
    ///
    /// Blockchain geçici olarak erişilemez olabilir. Sonsuz döngüye girmemek
    /// için deneme sayısını takip ediyoruz; belirli bir eşikten sonra
    /// kayıt es geçilir ve elle müdahale gerekir.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>Son denemede alınan hata mesajı (varsa).</summary>
    public string? LastError { get; set; }
}