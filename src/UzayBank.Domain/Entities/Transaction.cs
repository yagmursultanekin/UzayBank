using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UzayBank.Domain.Enums;

namespace UzayBank.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }

    /// <summary>
    /// İşlemin kalıcı, veritabanından bağımsız kimliği.
    ///
    /// NEDEN Id YETMİYOR:
    /// Id veritabanı tarafından kayıt sırasında atanır. Hash hesaplamak için
    /// kimliğe ihtiyacımız var, ama hash'i kayıttan ÖNCE hesaplamak istiyoruz —
    /// aksi hâlde iki ayrı SaveChanges gerekir. GUID'i uygulama üretebildiği
    /// için bu sorun ortadan kalkar.
    ///
    /// Ayrıca blockchain katmanında işlemi dışarıdan referanslarken kalıcı bir
    /// kimlik gerekiyor; Id veritabanına özgüdür, veritabanı sıfırlanırsa
    /// anlamını yitirir.
    /// </summary>
    public Guid TransactionRef { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal BalanceAfterTransaction { get; set; }

    /// <summary>
    /// Bu işlem kaydının SHA-256 parmak izi.
    ///
    /// Kaydın alanlarından (tutar, tarih, tip, açıklama vb.) hesaplanır.
    /// Kayıt sonradan değiştirilirse, yeniden hesaplanan hash bu değerle
    /// tutmaz — kurcalama böyle tespit edilir.
    /// </summary>
    public string? TxHash { get; set; }

    /// <summary>
    /// Aynı hesaptaki bir önceki işlemin TxHash değeri.
    ///
    /// Bu alan işlemleri birbirine bağlar. Ortadaki bir kaydı değiştirmek,
    /// sonraki tüm kayıtların hash'ini de geçersiz kılar.
    ///
    /// Hesabın ilk işleminde "GENESIS" değerini alır.
    /// </summary>
    public string? PreviousTxHash { get; set; }
}