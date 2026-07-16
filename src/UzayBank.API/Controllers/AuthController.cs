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
            return BadRequest(new { code = "EMAIL_DOMAIN_NOT_ALLOWED" });

        var result = await _authService.RegisterAsync(registerDto);
        if (result == null)
            return BadRequest(new { code = "EMAIL_ALREADY_EXISTS" });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        if (result == null)
            return Unauthorized(new { code = "INVALID_CREDENTIALS" });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]   // Çıkış yapmak için geçerli bir token gerekli
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (jti == null || expClaim == null)
            return BadRequest(new { code = "INVALID_TOKEN" });

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;

        await _authService.LogoutAsync(jti, expiresAt);
        return Ok(new { code = "LOGOUT_SUCCESS" });
    }
}