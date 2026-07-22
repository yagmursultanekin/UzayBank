using Microsoft.EntityFrameworkCore;
using UzayBank.Infrastructure.Persistence;

namespace UzayBank.Tests;

/// <summary>
/// Testler için gerçek bir SQL Server veritabanı hazırlar.
///
/// NEDEN GERÇEK VERİTABANI?
/// Test ettiğimiz şey (RowVersion / optimistic concurrency) tamamen
/// SQL Server'ın bir özelliği. In-memory veya SQLite sağlayıcısı RowVersion'ı
/// desteklemez — test "geçer" ama gerçekte hiçbir şey doğrulamamış oluruz.
/// Bu yüzden yavaş da olsa gerçek veritabanı kullanıyoruz.
///
/// Veritabanı adı üretimdekinden farklı (UzayBankDb_Test) — testler
/// geliştirme verisini asla silmemeli.
/// </summary>
public class DatabaseFixture : IDisposable
{
    private const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=UzayBankDb_Test;Trusted_Connection=True;TrustServerCertificate=True;";

    public DatabaseFixture()
    {
        // Her test çalışmasından önce veritabanını sıfırdan oluşturuyoruz.
        // Böylece testler önceki çalışmadan kalan veriye bağımlı olmaz.
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Yeni bir DbContext örneği üretir.
    ///
    /// ÖNEMLİ: Eşzamanlılık testinde iki AYRI context gerekiyor.
    /// Aynı context'i paylaşırsak EF Core'un change tracker'ı entity'leri
    /// önbellekte tutar ve iki "eşzamanlı" istek aslında aynı nesneyi
    /// paylaşır — çakışma hiç oluşmaz, test yalan söyler.
    /// Ayrı context'ler iki ayrı HTTP isteğini temsil eder.
    /// </summary>
    public UzayBankDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UzayBankDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new UzayBankDbContext(options);
    }

    public void Dispose()
    {
        // Test bitince veritabanını silmiyoruz — hata ayıklarken içine
        // bakabilmek faydalı. Bir sonraki çalışmada nasılsa sıfırlanacak.
        GC.SuppressFinalize(this);
    }
}