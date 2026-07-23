using UzayBank.Application.DTOs;

namespace UzayBank.Application.Interfaces;

/// <summary>
/// İşlem kayıtlarının bütünlüğünü doğrular.
///
/// Şu an yalnızca veritabanı içi hash zincirini kontrol ediyor.
/// Blockchain katmanı eklendiğinde, zincirin son halkasının Ethereum'daki
/// kayıtla eşleşip eşleşmediği de burada doğrulanacak.
/// </summary>
public interface IIntegrityService
{
    /// <summary>
    /// Bir hesabın tüm işlem zincirini doğrular.
    /// Hesap kullanıcıya ait değilse null döner.
    /// </summary>
    Task<AccountIntegrityDto?> VerifyAccountAsync(int accountId, int userId);
}