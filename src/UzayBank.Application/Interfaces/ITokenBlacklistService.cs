namespace UzayBank.Application.Interfaces;

/// <summary>
/// Çıkış yapılmış (iptal edilmiş) JWT token'ları tutar.
/// JWT stateless olduğu için sunucu tarafında iptal listesi gerekir.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>Token'ı kara listeye alır. expiresAt: token'ın kendi son kullanma tarihi.</summary>
    void Revoke(string jti, DateTime expiresAt);

    /// <summary>Token iptal edilmiş mi?</summary>
    bool IsRevoked(string jti);
}