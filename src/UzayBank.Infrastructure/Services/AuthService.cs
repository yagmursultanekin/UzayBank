using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using UzayBank.Domain.Entities;
using UzayBank.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UzayBank.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UzayBankDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ITokenBlacklistService _blacklist;

    public AuthService(UzayBankDbContext context, IConfiguration configuration, ITokenBlacklistService blacklist)
    {
        _context = context;
        _configuration = configuration;
        _blacklist = blacklist;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        // Email daha önce kayıtlı mı kontrol et
        if (_context.Users.Any(u => u.Email == registerDto.Email))
            return null;

        // Şifreyi hash'le
        var user = new User
        {
            FullName = registerDto.FullName,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateToken(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        // Kullanıcıyı bul
        var user = _context.Users.FirstOrDefault(u => u.Email == loginDto.Email );
        if (user == null)
            return null;

        // Şifreyi doğrula
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return null;

        return await Task.FromResult(GenerateToken(user));
    }

    public async Task<bool> LogoutAsync(string jti, DateTime expiresAt)
    {
        _blacklist.Revoke(jti, expiresAt);
        return await Task.FromResult(true);
    }

    private AuthResponseDto GenerateToken(User user)
    {
        var secretKey = _configuration["Jwt:SecretKey"]!;
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiry = int.Parse(_configuration["Jwt:ExpiryInMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
          {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            // Her token'a benzersiz kimlik — çıkışta bu id kara listeye yazılacak
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: credentials
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FullName = user.FullName,
            Email = user.Email
        };
    }
}