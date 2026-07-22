using Microsoft.EntityFrameworkCore;
using UzayBank.Domain.Entities;

namespace UzayBank.Infrastructure.Persistence;

public class UzayBankDbContext : DbContext
{
    public UzayBankDbContext(DbContextOptions<UzayBankDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(entity =>
        {
            // Aynı hesap aynı kullanıcıya iki kez bağlanamaz.
            // Bu kısıtı koda değil veritabanına yaptırıyoruz —
            // kod hata yapabilir, DB kısıtı yapamaz.
            entity.HasIndex(ua => new { ua.UserId, ua.AccountNumber }).IsUnique();

            entity.Property(ua => ua.AccountNumber).HasMaxLength(34).IsRequired();
            entity.Property(ua => ua.IBAN).HasMaxLength(34);
            entity.Property(ua => ua.Currency).HasMaxLength(3);

            // Kullanıcı silinirse eşlemeleri de silinir — yetim kayıt kalmaz.
            entity.HasOne(ua => ua.User)
                  .WithMany()
                  .HasForeignKey(ua => ua.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            // Para alanlarında precision'ı açıkça belirtiyoruz.
            // Belirtilmezse EF Core varsayılan olarak decimal(18,2) kullanır ve
            // migration sırasında "sessiz kesme olabilir" uyarısı verir.
            // Değer aynı olsa bile açık yazmak, kararın bilinçli olduğunu gösterir
            // ve EF sürümü değişince davranışın sessizce kaymasını engeller.
            entity.Property(a => a.Balance).HasPrecision(18, 2);

            entity.Property(a => a.AccountNumber).HasMaxLength(34).IsRequired();
            entity.Property(a => a.IBAN).HasMaxLength(34).IsRequired();
            entity.Property(a => a.Currency).HasMaxLength(3).IsRequired();
            entity.Property(a => a.AccountHolderName).HasMaxLength(200).IsRequired();

            // Transfer, alıcıyı IBAN ile buluyor. IBAN benzersiz değilse
            // FirstOrDefaultAsync sessizce ilk eşleşeni seçer — para yanlış
            // hesaba gidebilir. Bu yüzden kısıtı veritabanına koyuyoruz.
            entity.HasIndex(a => a.IBAN).IsUnique();
            entity.HasIndex(a => a.AccountNumber).IsUnique();

            // Kullanıcının hesaplarını listeleme en sık yapılan sorgu.
            entity.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.BalanceAfterTransaction).HasPrecision(18, 2);

            // Açıklama sınırsız büyümesin — transfer açıklaması alıcı adıyla
            // birleştiği için uzayabiliyor.
            entity.Property(t => t.Description).HasMaxLength(500);

            // İşlem geçmişi her zaman hesap + tarih sırasıyla sorgulanıyor.
            entity.HasIndex(t => new { t.AccountId, t.TransactionDate });
        });
    }
}