using Microsoft.Extensions.Caching.Memory;
using UzayBank.Application.Interfaces;

namespace UzayBank.Infrastructure.Services;

/// <summary>
/// Bellek içi kara liste. Token'lar kendi son kullanma tarihlerinde
/// cache'ten otomatik düşer — süresi dolmuş token zaten geçersiz,
/// listede tutmanın anlamı yok.
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Revoke(string jti, DateTime expiresAt)
    {
        var remaining = expiresAt - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
            return;   // Zaten süresi dolmuş, listeye almaya gerek yok

        _cache.Set(Key(jti), true, remaining);
    }

    public bool IsRevoked(string jti) => _cache.TryGetValue(Key(jti), out _);

    private static string Key(string jti) => $"revoked_token:{jti}";
}