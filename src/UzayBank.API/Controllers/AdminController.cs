using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.DTOs;
using UzayBank.Application.Interfaces;

namespace UzayBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]   // TÜM controller sadece Admin'e açık
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>Tüm hesapları, atandıkları kullanıcıyla birlikte listeler.</summary>
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments()
    {
        var assignments = await _adminService.GetAllAssignmentsAsync();
        return Ok(assignments);
    }

    /// <summary>Bir hesabı bir kullanıcıya atar.</summary>
    [HttpPost("assign")]
    public async Task<IActionResult> Assign(AdminAssignDto dto)
    {
        var success = await _adminService.AssignAccountAsync(dto.UserId, dto.AccountNumber);

        // false → hesap zaten başkasına atanmış (tek kişiye kuralı)
        if (!success)
            return BadRequest(new { code = "ACCOUNT_ALREADY_ASSIGNED" });

        return Ok(new { code = "ASSIGN_SUCCESS" });
    }

    /// <summary>Bir hesabın atamasını kaldırır.</summary>
    [HttpPost("unassign")]
    public async Task<IActionResult> Unassign(AdminAssignDto dto)
    {
        await _adminService.UnassignAccountAsync(dto.AccountNumber);
        return Ok(new { code = "UNASSIGN_SUCCESS" });
    }
}