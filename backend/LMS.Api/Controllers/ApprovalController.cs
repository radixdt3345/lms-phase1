using LMS.Application.DTOs.Approval;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Api.Controllers;

/// <summary>
/// Approval Workflow endpoints — F-08 API layer (FR-66 to FR-69).
/// Implements the two-level leave approval chain. All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/approvals")]
[Authorize]
[Produces("application/json")]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(IApprovalService approvalService, ILogger<ApprovalController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// Returns pending approval actions for the current approver (Manager/HRAdmin) — FR-68.
    /// Manager: L1 records where they are the assigned approver and no action taken yet.
    /// HRAdmin: all L2 records that are still pending.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ApprovalRecordDto>>>> GetPending()
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();

        var pending = await _approvalService.GetPendingAsync(callerId, callerRole);
        return Ok(ApiResponse<IEnumerable<ApprovalRecordDto>>.Ok(pending));
    }

    /// <summary>
    /// Returns the approval history for the caller — Manager sees own actioned records,
    /// HRAdmin sees all actioned records.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ApprovalRecordDto>>>> GetHistory()
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();

        var history = await _approvalService.GetHistoryAsync(callerId, callerRole);
        return Ok(ApiResponse<IEnumerable<ApprovalRecordDto>>.Ok(history));
    }

    /// <summary>
    /// Escalates a leave request to HR Admin after L1 approval (Manager only) — FR-67.
    /// Transitions the request to WaitingL2 and creates an L2 ApprovalRecord.
    /// </summary>
    [HttpPost("{leaveRequestId:guid}/escalate")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ApiResponse<ApprovalRecordDto>>> Escalate(
        Guid leaveRequestId,
        [FromBody] EscalateRequestDto dto)
    {
        var managerId = GetCallerId();

        try
        {
            var record = await _approvalService.EscalateAsync(leaveRequestId, managerId, dto.Reason ?? string.Empty);
            return Ok(ApiResponse<ApprovalRecordDto>.Ok(record));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Escalate — leave request not found: {LeaveRequestId}", leaveRequestId);
            return NotFound(new ProblemDetails
            {
                Title = "Leave request not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_REQUEST_NOT_FOUND" }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Escalate — unauthorised: {ManagerId}", managerId);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden,
                Extensions = { ["error_code"] = "NOT_L1_APPROVER" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Escalate — invalid state for leave request {LeaveRequestId}", leaveRequestId);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid request state",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "INVALID_REQUEST_STATE" }
            });
        }
    }

    /// <summary>
    /// Returns approval stats (pending count, average turnaround time, totals) — FR-68 supporting data.
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<ApprovalStatsDto>>> GetStats()
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();

        var stats = await _approvalService.GetStatsAsync(callerId, callerRole);
        return Ok(ApiResponse<ApprovalStatsDto>.Ok(stats));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetCallerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("oid");

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string GetCallerRole()
    {
        // Azure AD emits roles in the "roles" claim
        if (User.IsInRole("HRAdmin") || User.IsInRole("SuperAdmin"))
            return "HRAdmin";
        if (User.IsInRole("Manager"))
            return "Manager";
        return "Employee";
    }
}
