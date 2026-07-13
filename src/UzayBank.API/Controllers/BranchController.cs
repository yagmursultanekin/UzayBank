using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UzayBank.Application.Interfaces;

namespace UzayBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet("nearest")]
    public async Task<IActionResult> GetNearest(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] int distance = 1)
    {
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
            return BadRequest("Geçersiz koordinat.");

        var branches = await _branchService.GetNearestAsync(lat, lng, distance);
        return Ok(branches);
    }
}