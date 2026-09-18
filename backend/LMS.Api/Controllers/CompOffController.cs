using LMS.Application.DTOs.CompOff;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Api.Controllers;

/// <summary>
/// Comp-Off Management endpoints — F-07 (FR-58 to FR-65).
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/comp-off-requests")]
[Authorize]
[Produces("application/json")]
public class CompOffController : ControllerBase
{
    private readonly ICompOffService _compOffService;
    private readonly ILogger<CompOffController> _logger;

    public CompOffController(ICompOffService compOffService, ILogger<CompOffController> logger)
    {
        _compOffService = compOffService;
        _logger = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetCallerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User identity not found in token.");
        return Guid.Parse(sub);
    }

    private string GetCallerRole()
    {
        if (User.IsInRole("HRAdmin")) return "HRAdmin";
        if (User.IsInRole("SuperAdmin")) return "SuperAdmin";
        if (User.IsInRole("Manager")) return "Manager";
        return "Employee";
    }

    // ── POST /api/comp-off-requests ───────────────────────────────────────────

    /// <summary>
    /// Submits a new comp-off request. Employee role only.
    /// AC-51: returns 422 if date_worked is a regular working weekday.
    /// AC-52: returns 422 if worked hours are less than 4.
    /// AC-53: returns 201 with credit=0.5 for [4h, 8h) half-day requests.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompOffRequestDto>>> Submit([FromBody] CreateCompOffRequestDto dto)
    {
        var callerId = GetCallerId();
        try
        {
            var result = await _compOffService.SubmitAsync(callerId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CompOffRequestDto>.Ok(result));
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("NOT_A_NON_WORKING_DAY"))
        {
            _logger.LogWarning(ex, "CompOff submit — not a non-working day");
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Not a non-working day",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "NOT_A_NON_WORKING_DAY" }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("INSUFFICIENT_HOURS"))
        {
            _logger.LogWarning(ex, "CompOff submit — insufficient hours");
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Insufficient worked hours",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "INSUFFICIENT_HOURS" }
            });
        }
    }

    // ── GET /api/comp-off-requests ────────────────────────────────────────────

    /// <summary>
    /// Lists comp-off requests scoped to caller's role:
    /// Employee = own, Manager = team, HRAdmin = all.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompOffRequestDto>>>> GetAll()
    {
        var callerId = GetCallerId();
        var role = GetCallerRole();
        var results = await _compOffService.GetListAsync(callerId, role);
        return Ok(ApiResponse<IEnumerable<CompOffRequestDto>>.Ok(results));
    }

    // ── GET /api/comp-off-requests/{id} ──────────────────────────────────────

    /// <summary>Returns a single comp-off request by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompOffRequestDto>>> GetById(Guid id)
    {
        var result = await _compOffService.GetByIdAsync(id);
        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Comp-off request not found",
                Detail = $"No active comp-off request with ID '{id}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "COMP_OFF_REQUEST_NOT_FOUND" }
            });
        }
        return Ok(ApiResponse<CompOffRequestDto>.Ok(result));
    }

    // ── POST /api/comp-off-requests/{id}/approve ──────────────────────────────

    /// <summary>
    /// Approves a pending comp-off request. Manager or HRAdmin only.
    /// AC-54: credits 0.5 or 1.0 day and creates CompOffCredit with 30-day expiry.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<CompOffRequestDto>>> Approve(Guid id)
    {
        var callerId = GetCallerId();
        var role = GetCallerRole();
        try
        {
            var result = await _compOffService.ApproveAsync(id, callerId, role);
            return Ok(ApiResponse<CompOffRequestDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Request not found", Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "COMP_OFF_REQUEST_NOT_FOUND" }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot approve", Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "INVALID_STATUS_TRANSITION" }
            });
        }
    }

    // ── POST /api/comp-off-requests/{id}/reject ───────────────────────────────

    /// <summary>
    /// Rejects a pending comp-off request with a mandatory reason.
    /// AC-55: returns 403 if the request owner tries to reject their own request.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<CompOffRequestDto>>> Reject(Guid id, [FromBody] RejectCompOffRequestDto dto)
    {
        var callerId = GetCallerId();
        var role = GetCallerRole();
        try
        {
            var result = await _compOffService.RejectAsync(id, callerId, role, dto.RejectionReason);
            return Ok(ApiResponse<CompOffRequestDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Request not found", Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "COMP_OFF_REQUEST_NOT_FOUND" }
            });
        }
        catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("SELF_CANCEL"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Self-cancellation not allowed",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden,
                Extensions = { ["error_code"] = "SELF_CANCEL_NOT_ALLOWED" }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot reject", Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "INVALID_STATUS_TRANSITION" }
            });
        }
    }
}

/// <summary>
/// Comp-Off Credits endpoints — GET /api/comp-off-credits.
/// Separate route prefix as credits are a different resource.
/// </summary>
[ApiController]
[Route("api/comp-off-credits")]
[Authorize]
[Produces("application/json")]
public class CompOffCreditsController : ControllerBase
{
    private readonly ICompOffService _compOffService;

    public CompOffCreditsController(ICompOffService compOffService)
    {
        _compOffService = compOffService;
    }

    private Guid GetCallerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User identity not found in token.");
        return Guid.Parse(sub);
    }

    /// <summary>Returns comp-off credit balance for the currently authenticated employee.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CompOffCreditDto>>> GetMyCredits()
    {
        var callerId = GetCallerId();
        var result = await _compOffService.GetCreditsAsync(callerId);
        return Ok(ApiResponse<CompOffCreditDto>.Ok(result));
    }

    /// <summary>
    /// Returns comp-off credit balance for any employee. HRAdmin/Manager only.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin,Manager")]
    public async Task<ActionResult<ApiResponse<CompOffCreditDto>>> GetCreditsForEmployee(Guid employeeId)
    {
        var result = await _compOffService.GetCreditsAsync(employeeId);
        return Ok(ApiResponse<CompOffCreditDto>.Ok(result));
    }
}
