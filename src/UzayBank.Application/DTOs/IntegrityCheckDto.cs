namespace UzayBank.Application.DTOs;

/// <summary>
/// Tek bir işlem kaydının bütünlük kontrolü sonucu.
/// </summary>
public class TransactionIntegrityDto
{
    public int TransactionId { get; set; }
    public Guid TransactionRef { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Kaydın veritabanında saklanan hash'i.</summary>
    public string? StoredHash { get; set; }

    /// <summary>Kaydın alanlarından şu an yeniden hesaplanan hash.</summary>
    public string? ComputedHash { get; set; }

    /// <summary>
    /// Kaydın kendi içeriği bozulmamış mı?
    /// StoredHash ile ComputedHash eşleşiyorsa true.
    /// </summary>
    public bool IsHashValid { get; set; }

    /// <summary>
    /// Zincir bağlantısı sağlam mı?
    /// Bu kaydın PreviousTxHash'i, bir önceki kaydın TxHash'i ile eşleşiyorsa true.
    /// </summary>
    public bool IsChainValid { get; set; }

    /// <summary>
    /// Kaydın hash'i hiç hesaplanmamışsa true.
    /// Hash alanları eklenmeden önce oluşmuş eski kayıtlar için geçerli —
    /// bu bir kurcalama belirtisi DEĞİL, sadece kapsam dışı olduklarını gösterir.
    /// </summary>
    public bool IsUnhashed { get; set; }
}

/// <summary>
/// Bir hesabın tüm işlem zincirinin doğrulama sonucu.
/// </summary>
public class AccountIntegrityDto
{
    public int AccountId { get; set; }

    /// <summary>Kontrol edilen toplam kayıt sayısı.</summary>
    public int TotalTransactions { get; set; }

    /// <summary>Hash'i olmayan (kapsam dışı) kayıt sayısı.</summary>
    public int UnhashedCount { get; set; }

    /// <summary>Sorun tespit edilen kayıt sayısı.</summary>
    public int InvalidCount { get; set; }

    /// <summary>Zincirin tamamı sağlam mı?</summary>
    public bool IsValid { get; set; }

    public List<TransactionIntegrityDto> Transactions { get; set; } = new();
}