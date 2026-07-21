using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UzayBank.Domain.Entities;
using UzayBank.Domain.Interfaces;

namespace UzayBank.Domain.Services;

/// <summary>
/// SHA-256 tabanlı işlem hash'leyici.
///
/// TASARIM KURALI: Bu sınıfın çıktısı ASLA değişmemelidir.
/// Format değişirse geçmişteki tüm hash'ler geçersiz olur ve
/// doğrulama "kurcalanmış" sonucu verir. Yeni bir format gerekirse
/// bu sınıfı değiştirmeyin — yeni bir versiyon sınıfı yazın.
/// </summary>
public class TransactionHasher : ITransactionHasher
{
    /// <summary>
    /// Bir hesabın ilk işleminde kullanılan başlangıç değeri.
    /// null veya boş string yerine açık bir işaretçi kullanıyoruz ki
    /// "zincirin başı" ile "hash eksik" durumu birbirine karışmasın.
    /// </summary>
    public const string GenesisHash = "GENESIS";

    /// <summary>
    /// Alanları ayıran karakter. Ayraç olmadan "AB" + "1" ile
    /// "A" + "B1" aynı metni üretir ve farklı kayıtlar aynı hash'i alır.
    /// </summary>
    private const char Separator = '|';

    public string BuildCanonicalString(Transaction transaction, string previousTxHash)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (string.IsNullOrWhiteSpace(previousTxHash))
            previousTxHash = GenesisHash;

        var builder = new StringBuilder();

        // ALAN SIRASI SABİTTİR. Sıra değişirse hash değişir.
        builder.Append(transaction.Id.ToString(CultureInfo.InvariantCulture));
        builder.Append(Separator);

        builder.Append(transaction.AccountId.ToString(CultureInfo.InvariantCulture));
        builder.Append(Separator);

        // Para: InvariantCulture + sabit 2 basamak.
        // Türkçe kültürde "80,50", invariant'ta "80.50" olur — sabitlemezsek
        // aynı kayıt farklı makinede farklı hash üretir.
        builder.Append(transaction.Amount.ToString("F2", CultureInfo.InvariantCulture));
        builder.Append(Separator);

        // Enum: sayı yerine isim. Enum'a ortadan yeni değer eklenirse
        // sayılar kayar, isimler kaymaz.
        builder.Append(transaction.Type.ToString());
        builder.Append(Separator);

        // Description null olabilir; boş string'e normalize ediyoruz.
        builder.Append(transaction.Description ?? string.Empty);
        builder.Append(Separator);

        // Tarih: önce UTC'ye çevir, sonra ISO-8601 ("o" formatı).
        // Saat dilimi ve kültür bağımlılığını bu şekilde kaldırıyoruz.
        builder.Append(NormalizeDate(transaction.TransactionDate));
        builder.Append(Separator);

        builder.Append(transaction.BalanceAfterTransaction.ToString("F2", CultureInfo.InvariantCulture));
        builder.Append(Separator);

        // Zincir halkası: bu alan sayesinde geçmiş bir kaydı değiştirmek
        // sonraki tüm hash'leri bozar.
        builder.Append(previousTxHash);

        return builder.ToString();
    }

    public string ComputeHash(Transaction transaction, string previousTxHash)
    {
        var canonical = BuildCanonicalString(transaction, previousTxHash);
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hashBytes = SHA256.HashData(bytes);

        // Convert.ToHexString büyük harfli hex üretir (ör. "A3F2...").
        // Karşılaştırmalarda büyük/küçük harf tutarsızlığı olmasın diye
        // her yerde bu formatı kullanacağız.
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Tarihi UTC'ye çevirip kültürden bağımsız ISO-8601 formatına getirir.
    /// Kind bilgisi Unspecified ise (EF Core'dan okunan tarihler genelde böyledir)
    /// UTC varsayıyoruz — aksi hâlde makinenin saat dilimine göre kayar.
    /// </summary>
    private static string NormalizeDate(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return utc.ToString("o", CultureInfo.InvariantCulture);
    }
}