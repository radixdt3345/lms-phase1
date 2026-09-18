using LMS.Application.DTOs.LeaveBalance;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Api.Controllers;

/// <summary>
/// Leave Balance Management — F-05 API layer (FR-33 to FR-41).
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/leave-balances")]
[Authorize]
[Produces("application/json")]
public class LeaveBalanceController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly ILogger<LeaveBalanceController> _logger;

    public LeaveBalanceController(ILeaveBalanceService leaveBalanceService, ILogger<LeaveBalanceController> logger)
    {
        _leaveBalanceService = leaveBalanceService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all leave balances across all employees.
    /// Restricted to HRAdmin only.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaveBalanceDto>>>> GetAll()
    {
        var balances = await _leaveBalanceService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(balances));
    }

    /// <summary>
    /// Returns the current employee's leave balances.
    /// The employee ID is derived from the JWT claim.
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaveBalanceDto>>>> GetMy()
    {
        var employeeId = GetCurrentUserId();
        if (employeeId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Cannot determine employee identity from token.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions = { ["error_code"] = "INVALID_TOKEN_IDENTITY" }
            });
        }

        var balances = await _leaveBalanceService.GetByEmployeeIdAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(balances));
    }

    /// <summary>
    /// Returns the leave balances for a specific employee.
    /// Restricted to Manager and HRAdmin.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaveBalanceDto>>>> GetByEmployee(Guid employeeId)
    {
        var balances = await _leaveBalanceService.GetByEmployeeIdAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(balances));
    }

    /// <summary>
    /// Applies a manual adjustment (positive or negative) to an employee's leave balance.
    /// Restricted to HRAdmin only.
    /// </summary>
    [HttpPost("adjust")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ApiResponse<LeaveBalanceDto>>> Adjust([FromBody] AdjustBalanceDto dto)
    {
        try
        {
            var balance = await _leaveBalanceService.AdjustBalanceAsync(dto);
            return Ok(ApiResponse<LeaveBalanceDto>.Ok(balance));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "AdjustBalance — balance record not found");
            return NotFound(new ProblemDetails
            {
                Title = "Leave balance not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_BALANCE_NOT_FOUND" }
            });
        }
    }

    /// <summary>
    /// Returns aggregate summary statistics for all leave balances in the current year.
    /// Restricted to HRAdmin only.
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ApiResponse<LeaveBalanceSummaryDto>>> GetSummary()
    {
        var summary = await _leaveBalanceService.GetSummaryAsync();
        return Ok(ApiResponse<LeaveBalanceSummaryDto>.Ok(summary));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the current user's internal Guid from the JWT.
    /// Azure AD emits the user object ID in the "oid" claim; the NameIdentifier
    /// fallback covers local/test tokens.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue("oid")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? string.Empty;

        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
