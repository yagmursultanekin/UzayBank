using UzayBank.Application.DTOs;

namespace UzayBank.Application.Interfaces;

public interface IAdminService
{
    /// <summary>Tüm VakıfBank hesaplarını, atanmış oldukları kullanıcıyla birlikte listeler.</summary>
    Task<List<AccountAssignmentDto>> GetAllAssignmentsAsync();

    /// <summary>
    /// Bir hesabı bir kullanıcıya atar.
    /// Hesap zaten başkasına atanmışsa false döner (tek kişiye kuralı).
    /// </summary>
    Task<bool> AssignAccountAsync(int userId, string accountNumber);

    /// <summary>Bir hesabın atamasını kaldırır.</summary>
    Task UnassignAccountAsync(string accountNumber);

    /// <summary>Tüm kullanıcıları listeler (admin, hesap atarken seçim yapabilsin).</summary>
    Task<List<UserListDto>> GetAllUsersAsync();
}