using LMS.Application.DTOs.Holiday;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Public holiday management endpoints (F-10 — F10-API-001).
/// </summary>
[ApiController]
[Route("api/public-holidays")]
[Authorize]
[Produces("application/json")]
public class PublicHolidayController : ControllerBase
{
    private readonly IPublicHolidayService _service;
    private readonly ILogger<PublicHolidayController> _logger;

    public PublicHolidayController(IPublicHolidayService service, ILogger<PublicHolidayController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Returns all public holidays for the given calendar year.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicHolidayDto>>>> GetByYear(
        [FromQuery] int year)
    {
        var holidays = await _service.GetByYearAsync(year);
        return Ok(ApiResponse<IEnumerable<PublicHolidayDto>>.Ok(holidays));
    }

    /// <summary>
    /// Returns the public holiday with the given ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> GetById(Guid id)
    {
        var holiday = await _service.GetByIdAsync(id);

        if (holiday is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Holiday not found",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "HOLIDAY_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<PublicHolidayDto>.Ok(holiday));
    }

    /// <summary>
    /// Returns the number of working days between two dates (inclusive).
    /// Saturdays, Sundays, and active public holidays are excluded.
    /// </summary>
    [HttpGet("working-days")]
    public async Task<ActionResult<ApiResponse<int>>> GetWorkingDays(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var count = await _service.GetWorkingDaysAsync(from, to);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>
    /// Returns true when the given date is an active public holiday.
    /// </summary>
    [HttpGet("is-holiday")]
    public async Task<ActionResult<ApiResponse<bool>>> IsHoliday([FromQuery] DateOnly date)
    {
        var result = await _service.IsHolidayAsync(date);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// Creates a new public holiday. Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> Create(
        [FromBody] CreatePublicHolidayDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<PublicHolidayDto>.Ok(created));
    }

    /// <summary>
    /// Bulk-imports public holidays. Duplicate Name+Date pairs are skipped.
    /// Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicHolidayDto>>>> BulkImport(
        [FromBody] IEnumerable<CreatePublicHolidayDto> dtos)
    {
        var created = await _service.BulkImportAsync(dtos);
        return Ok(ApiResponse<IEnumerable<PublicHolidayDto>>.Ok(created));
    }

    /// <summary>
    /// Partially updates an existing public holiday. Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> Update(
        Guid id,
        [FromBody] UpdatePublicHolidayDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<PublicHolidayDto>.Ok(updated));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Update holiday — {Id} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Holiday not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "HOLIDAY_NOT_FOUND" }
            });
        }
    }

    /// <summary>
    /// Hard-deletes a public holiday. Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Holiday not found",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "HOLIDAY_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
