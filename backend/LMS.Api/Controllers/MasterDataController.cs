using LMS.Application.DTOs.MasterData;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Master data management endpoints (F-13 — F13-API-001).
/// Provides seed-status queries, manual re-seed triggers, and system config management.
/// </summary>
[ApiController]
[Route("api/master-data")]
[Authorize]
[Produces("application/json")]
public class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _service;
    private readonly ILogger<MasterDataController> _logger;

    public MasterDataController(IMasterDataService service, ILogger<MasterDataController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current seed status — entity counts and overall health flag.
    /// </summary>
    [HttpGet("seed-status")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SeedStatusDto>>> GetSeedStatus(CancellationToken cancellationToken)
    {
        var status = await _service.GetSeedStatusAsync(cancellationToken);
        return Ok(ApiResponse<SeedStatusDto>.Ok(status));
    }

    /// <summary>
    /// Triggers a manual re-seed of master data for the specified section.
    /// </summary>
    [HttpPost("reseed")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> TriggerReseed(
        [FromBody] ReseedRequestDto request,
        CancellationToken cancellationToken)
    {
        var success = await _service.TriggerReseedAsync(request.Section, cancellationToken);

        if (!success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid section",
                Detail = $"Section '{request.Section}' is not a valid reseed target. Valid values: roles, departments, leave-types, holidays, all"
            });
        }

        return Ok(ApiResponse<bool>.Ok(success));
    }

    /// <summary>
    /// Returns all system configuration key/value pairs.
    /// </summary>
    [HttpGet("system-config")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemConfigDto>>>> GetSystemConfigs(CancellationToken cancellationToken)
    {
        var configs = await _service.GetSystemConfigsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<SystemConfigDto>>.Ok(configs));
    }

    /// <summary>
    /// Returns a single system configuration value by key.
    /// </summary>
    [HttpGet("system-config/{key}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SystemConfigDto>>> GetSystemConfig(string key, CancellationToken cancellationToken)
    {
        var config = await _service.GetSystemConfigAsync(key, cancellationToken);

        if (config is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Config key not found",
                Detail = $"No system config found with key '{key}'."
            });
        }

        return Ok(ApiResponse<SystemConfigDto>.Ok(config));
    }

    /// <summary>
    /// Updates the value of a system configuration key. Only editable configs may be changed.
    /// </summary>
    [HttpPut("system-config/{key}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SystemConfigDto>>> UpdateSystemConfig(
        string key,
        [FromBody] UpdateSystemConfigDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateSystemConfigAsync(key, dto, cancellationToken);
            return Ok(ApiResponse<SystemConfigDto>.Ok(updated));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Config key not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Title = "Config not editable", Detail = ex.Message });
        }
    }
}
