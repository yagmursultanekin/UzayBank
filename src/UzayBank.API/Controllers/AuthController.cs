using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;

namespace UzayBank.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
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
    }

