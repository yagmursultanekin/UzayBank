using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace UzayBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        // Kurumsal e-posta kontrolü: yalnızca izin verilen alan adı kabul edilir
        var allowedDomain = _configuration["Auth:AllowedEmailDomain"] ?? "uzaybank.com";
        var email = registerDto.Email?.Trim() ?? "";

        if (!email.EndsWith($"@{allowedDomain}", StringComparison.OrdinalIgnoreCase))
            return BadRequest($"Yalnızca @{allowedDomain} uzantılı e-posta adresleri ile kayıt olunabilir.");

        var result = await _authService.RegisterAsync(registerDto);
        if (result == null)
            return BadRequest("Bu e-posta adresi zaten kayıtlı.");

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        if (result == null)
            return Unauthorized("E-posta veya şifre hatalı.");

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]   // Çıkış yapmak için geçerli bir token gerekli
    public async Task<IActionResult> Logout()
    {
        // Token'ın kimliği ve son kullanma tarihi — ikisi de token'ın içinde
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (jti == null || expClaim == null)
            return BadRequest("Token bilgisi okunamadı.");

        // exp, Unix timestamp (saniye) olarak gelir
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;

        await _authService.LogoutAsync(jti, expiresAt);
        return Ok(new { message = "Çıkış yapıldı." });
    }
}