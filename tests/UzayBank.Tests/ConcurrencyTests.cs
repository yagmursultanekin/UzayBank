using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UzayBank.Domain.Entities;
using UzayBank.Domain.Enums;

namespace UzayBank.Tests;

/// <summary>
/// RowVersion (optimistic concurrency) davranışını doğrular.
///
/// Bu testler manuel olarak yapılamaz: iki isteği tam olarak aynı anda
/// göndermek gerekir. Test kodu bunu iki ayrı DbContext ile simüle eder.
/// </summary>
public class ConcurrencyTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public ConcurrencyTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EszamanliIkiCekim_IkincisiCakismaHatasiVermeli()
    {
        // --- HAZIRLIK ---
        // 100 TL bakiyeli bir hesap oluşturuyoruz.
        int accountId;
        using (var setup = _fixture.CreateContext())
        {
            var user = new User
            {
                FullName = "Test Kullanıcı",
                Email = $"test-{Guid.NewGuid()}@uzaybank.com",
                PasswordHash = "dummy",
                CreatedAt = DateTime.UtcNow
            };
            setup.Users.Add(user);
            await setup.SaveChangesAsync();

            var account = new Account
            {
                UserId = user.Id,
                AccountNumber = $"9{Random.Shared.NextInt64(100000000000000, 999999999999999)}",
                IBAN = $"TR99{Guid.NewGuid():N}"[..26],
                Currency = "TL",
                Balance = 100m,
                AccountHolderName = user.FullName,
                CreatedAt = DateTime.UtcNow,
                Source = AccountSource.UzayBank
            };
            setup.Accounts.Add(account);
            await setup.SaveChangesAsync();

            accountId = account.Id;
        }

        // --- İKİ EŞZAMANLI İSTEK ---
        // İki ayrı context = iki ayrı HTTP isteği.
        using var contextA = _fixture.CreateContext();
        using var contextB = _fixture.CreateContext();

        // İkisi de hesabı okuyor — ikisi de 100 TL görüyor ve
        // ikisi de aynı RowVersion değerini alıyor.
        var accountA = await contextA.Accounts.FirstAsync(a => a.Id == accountId);
        var accountB = await contextB.Accounts.FirstAsync(a => a.Id == accountId);

        accountA.Balance.Should().Be(100m);
        accountB.Balance.Should().Be(100m);

        // İkisi de 80 TL çekmek istiyor.
        accountA.Balance -= 80m;
        accountB.Balance -= 80m;

        // --- BİRİNCİ KAYIT: BAŞARILI ---
        await contextA.SaveChangesAsync();

        // --- İKİNCİ KAYIT: ÇAKIŞMA ---
        // B'nin elindeki RowVersion artık eski. EF Core'un ürettiği
        // UPDATE ... WHERE RowVersion = <eski> koşulu hiçbir satırı
        // etkilemez ve DbUpdateConcurrencyException fırlar.
        var act = async () => await contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // --- DOĞRULAMA ---
        // Bakiye 20 TL olmalı (100 - 80), -60 değil.
        // RowVersion olmasaydı ikinci kayıt da geçer ve 160 TL çekilmiş olurdu.
        using var verify = _fixture.CreateContext();
        var finalAccount = await verify.Accounts.FirstAsync(a => a.Id == accountId);
        finalAccount.Balance.Should().Be(20m);
    }

    [Fact]
    public async Task TekBasinaCekim_SorunsuzCalismali()
    {
        // Kontrol testi: çakışma yokken normal akış bozulmamalı.
        int accountId;
        using (var setup = _fixture.CreateContext())
        {
            var user = new User
            {
                FullName = "Test Kullanıcı 2",
                Email = $"test-{Guid.NewGuid()}@uzaybank.com",
                PasswordHash = "dummy",
                CreatedAt = DateTime.UtcNow
            };
            setup.Users.Add(user);
            await setup.SaveChangesAsync();

            var account = new Account
            {
                UserId = user.Id,
                AccountNumber = $"9{Random.Shared.NextInt64(100000000000000, 999999999999999)}",
                IBAN = $"TR99{Guid.NewGuid():N}"[..26],
                Currency = "TL",
                Balance = 500m,
                AccountHolderName = user.FullName,
                CreatedAt = DateTime.UtcNow,
                Source = AccountSource.UzayBank
            };
            setup.Accounts.Add(account);
            await setup.SaveChangesAsync();

            accountId = account.Id;
        }

        using var context = _fixture.CreateContext();
        var acc = await context.Accounts.FirstAsync(a => a.Id == accountId);
        acc.Balance -= 200m;
        await context.SaveChangesAsync();

        using var verify = _fixture.CreateContext();
        var final = await verify.Accounts.FirstAsync(a => a.Id == accountId);
        final.Balance.Should().Be(300m);
    }
}