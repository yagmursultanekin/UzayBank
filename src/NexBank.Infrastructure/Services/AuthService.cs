using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NexBank.Application.DTOs;
using NexBank.Application.Interfaces;
using NexBank.Domain.Entities;
using NexBank.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexBank.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly NexBankDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(NexBankDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
            new Claim(ClaimTypes.Name, user.FullName)
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