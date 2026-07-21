using UzayBank.Domain.Entities;

namespace UzayBank.Domain.Interfaces;

/// <summary>
/// Bir işlem kaydının değişmezlik parmak izini (SHA-256) üretir.
/// Saf bir kuraldır: aynı girdi her ortamda, her zaman aynı çıktıyı verir.
/// </summary>
public interface ITransactionHasher
{
    /// <summary>
    /// İşlemin hash'e girecek kanonik (standartlaştırılmış) metin halini üretir.
    /// Doğrulama ekranında "neyin hash'lendiğini" göstermek için de kullanılır.
    /// </summary>
    string BuildCanonicalString(Transaction transaction, string previousTxHash);

    /// <summary>
    /// İşlemin SHA-256 hash'ini büyük harfli hex string olarak döndürür.
    /// </summary>
    string ComputeHash(Transaction transaction, string previousTxHash);
}