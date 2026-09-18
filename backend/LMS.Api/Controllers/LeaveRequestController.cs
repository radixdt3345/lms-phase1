using LMS.Application.DTOs.LeaveRequest;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Api.Controllers;

/// <summary>
/// Leave Application and Workflow — F-06 API layer (FR-42 to FR-57, FR-70).
/// Covers the full leave request lifecycle.
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/leave-requests")]
[Authorize]
[Produces("application/json")]
public class LeaveRequestController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILogger<LeaveRequestController> _logger;

    public LeaveRequestController(ILeaveRequestService leaveRequestService, ILogger<LeaveRequestController> logger)
    {
        _leaveRequestService = leaveRequestService;
        _logger = logger;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits a new leave request (or saves as Draft when SaveAsDraft = true).
    /// Balance validation is applied on submission only; drafts do not deduct balance (FR-43).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var employeeId = GetCurrentUserId();
        if (employeeId == Guid.Empty)
            return Unauthorized(InvalidTokenProblem());

        try
        {
            var request = await _leaveRequestService.CreateAsync(employeeId, dto);
            return CreatedAtAction(nameof(GetById), new { id = request.Id },
                ApiResponse<LeaveRequestDto>.Ok(request));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "CreateLeaveRequest — resource not found");
            return NotFound(new ProblemDetails
            {
                Title = "Resource not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CreateLeaveRequest — validation failure: {Message}", ex.Message);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Leave request validation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Returns leave requests scoped by role:
    /// Employee → own, Manager → team, HRAdmin → all.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaveRequestDto>>>> GetAll()
    {
        var requesterId = GetCurrentUserId();
        if (requesterId == Guid.Empty)
            return Unauthorized(InvalidTokenProblem());

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
                    .Concat(User.FindAll("roles").Select(c => c.Value))
                    .Distinct();

        var requests = await _leaveRequestService.GetAllAsync(requesterId, roles);
        return Ok(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(requests));
    }

    /// <summary>Returns a single leave request by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> GetById(Guid id)
    {
        var request = await _leaveRequestService.GetByIdAsync(id);
        if (request is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Leave request not found",
                Detail = $"No leave request with ID '{id}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_REQUEST_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<LeaveRequestDto>.Ok(request));
    }

    // ── Employee actions ──────────────────────────────────────────────────────

    /// <summary>
    /// Cancels a pending leave request raised by the current employee.
    /// Restores the full balance immediately (FR-51).
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Cancel(Guid id)
    {
        var employeeId = GetCurrentUserId();
        if (employeeId == Guid.Empty)
            return Unauthorized(InvalidTokenProblem());

        try
        {
            var request = await _leaveRequestService.CancelAsync(id, employeeId);
            return Ok(ApiResponse<LeaveRequestDto>.Ok(request));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "CancelLeaveRequest — not found");
            return NotFound(new ProblemDetails
            {
                Title = "Leave request not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_REQUEST_NOT_FOUND" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CancelLeaveRequest — invalid state: {Message}", ex.Message);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot cancel leave request",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "CANCEL_NOT_ALLOWED" }
            });
        }
    }

    // ── Approver actions ──────────────────────────────────────────────────────

    /// <summary>
    /// Approves a leave request. Advances status through the state machine (FR-48).
    /// Restricted to Manager and HRAdmin.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Approve(Guid id)
    {
        var approverId = GetCurrentUserId();
        if (approverId == Guid.Empty)
            return Unauthorized(InvalidTokenProblem());

        try
        {
            var request = await _leaveRequestService.ApproveAsync(id, approverId);
            return Ok(ApiResponse<LeaveRequestDto>.Ok(request));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "ApproveLeaveRequest — not found");
            return NotFound(new ProblemDetails
            {
                Title = "Leave request not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_REQUEST_NOT_FOUND" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "ApproveLeaveRequest — invalid state: {Message}", ex.Message);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot approve leave request",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "APPROVE_NOT_ALLOWED" }
            });
        }
    }

    /// <summary>
    /// Rejects a leave request with a mandatory reason (AC-44).
    /// Restricted to Manager and HRAdmin.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Reject(Guid id, [FromBody] RejectLeaveRequestDto dto)
    {
        var approverId = GetCurrentUserId();
        if (approverId == Guid.Empty)
            return Unauthorized(InvalidTokenProblem());

        try
        {
            var request = await _leaveRequestService.RejectAsync(id, approverId, dto.Reason);
            return Ok(ApiResponse<LeaveRequestDto>.Ok(request));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "RejectLeaveRequest — not found");
            return NotFound(new ProblemDetails
            {
                Title = "Leave request not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_REQUEST_NOT_FOUND" }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "REJECTION_REASON_REQUIRED")
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Rejection reason required",
                Detail = "A non-empty rejection reason must be supplied.",
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "REJECTION_REASON_REQUIRED" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "RejectLeaveRequest — invalid state: {Message}", ex.Message);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot reject leave request",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["error_code"] = "REJECT_NOT_ALLOWED" }
            });
        }
    }

    // ── Pending queue ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns pending leave requests awaiting approval by the current approver.
    /// Restricted to Manager and HRAdmin.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,HRAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaveRequestDto>>>> GetPending()
    {
        var approverId = GetCurrentUserId();
        if (approverId == Guid.Empty)
            return Unauthorized(InvalidTokenProblem());

        var requests = await _leaveRequestService.GetPendingForApproverAsync(approverId);
        return Ok(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(requests));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue("oid")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? string.Empty;

        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static ProblemDetails InvalidTokenProblem() => new()
    {
        Title = "Unauthorized",
        Detail = "Cannot determine user identity from token.",
        Status = StatusCodes.Status401Unauthorized,
        Extensions = { ["error_code"] = "INVALID_TOKEN_IDENTITY" }
    };
}
