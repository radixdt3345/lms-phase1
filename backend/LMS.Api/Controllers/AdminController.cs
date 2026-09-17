using LMS.Application.DTOs.MasterData;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Admin utility endpoints for F-14 — Initial Data Seeding.
/// Provides seed-status queries and idempotent re-seed triggers for deployment engineers
/// and super-admins (US-14.1, FR-91, FR-92).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ISeedService _seedService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ISeedService seedService, ILogger<AdminController> logger)
    {
        _seedService = seedService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current seed status: entity counts for roles, departments, leave types,
    /// public holidays, system configs, and an IsHealthy flag (AC-68).
    /// Accessible by HRAdmin and SuperAdmin.
    /// </summary>
    [HttpGet("seed-status")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SeedStatusDto>>> GetSeedStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[F-14] GET /api/admin/seed-status requested");
        var status = await _seedService.GetSeedStatusAsync(cancellationToken);
        return Ok(ApiResponse<SeedStatusDto>.Ok(status));
    }

    /// <summary>
    /// Triggers an idempotent re-seed for the specified section (FR-92, AC-67).
    /// Safe to run multiple times — no duplicate records are created.
    /// Valid sections: roles | departments | leave-types | holidays | system-configs | all.
    /// Restricted to SuperAdmin only.
    /// </summary>
    [HttpPost("reseed")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> TriggerReseed(
        [FromBody] ReseedRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[F-14] POST /api/admin/reseed requested for section: {Section}", request.Section);

        var success = await _seedService.TriggerReseedAsync(request.Section, cancellationToken);

        if (!success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid section",
                Detail = $"Section '{request.Section}' is not a valid reseed target. " +
                         "Valid values: roles, departments, leave-types, holidays, system-configs, all",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(ApiResponse<bool>.Ok(success));
    }
}
