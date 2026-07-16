using UzayBank.Domain.Entities;

namespace UzayBank.Domain.Interfaces;

public interface IUserAccountRepository
{
    /// <summary>Kullanıcıya bağlı hesap numaralarını getirir.</summary>
    Task<List<string>> GetAccountNumbersByUserIdAsync(int userId);

    /// <summary>Bu hesap bu kullanıcıya bağlı mı?</summary>
    Task<bool> IsLinkedAsync(int userId, string accountNumber);

    /// <summary>Kullanıcıya hesap bağlar. Zaten bağlıysa hiçbir şey yapmaz.</summary>
    Task LinkAsync(int userId, string accountNumber, string iban, string currency);
    /// <summary>Bu hesap HERHANGİ bir kullanıcıya atanmış mı? (tek kişiye kuralı için)</summary>
    Task<bool> IsAccountTakenAsync(string accountNumber);

    /// <summary>Atamayı kaldırır.</summary>
    Task UnlinkAsync(string accountNumber);
}