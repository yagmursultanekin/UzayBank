using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UzayBank.Domain.Enums;

namespace UzayBank.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Bu hesap UzayBank'ın kendi (yapay) hesabı — VakıfBank'tan gelmiyor.
    // İleride farklı hesap türleri eklenirse ayrım için.
    public AccountSource Source { get; set; } = AccountSource.UzayBank;

    /// <summary>
    /// Eşzamanlılık damgası (optimistic concurrency).
    ///
    /// SQL Server bu kolonu otomatik yönetir: satır her değiştiğinde değeri artar.
    /// EF Core, UPDATE cümlesine "WHERE RowVersion = <okuduğum değer>" koşulunu ekler.
    /// Araya başka bir işlem girip satırı değiştirmişse koşul tutmaz, 0 satır
    /// etkilenir ve DbUpdateConcurrencyException fırlar.
    ///
    /// Bakiye için kritik: kontrol ve güncelleme tek bir SQL cümlesinde yapıldığı
    /// için "oku → kontrol et → yaz" arasındaki yarış penceresi kapanır.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
