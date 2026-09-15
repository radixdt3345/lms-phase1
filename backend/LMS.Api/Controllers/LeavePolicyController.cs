using LMS.Application.DTOs.LeavePolicy;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class LeavePolicyController : ControllerBase
{
    private readonly ILeavePolicyService _service;
    private readonly ILogger<LeavePolicyController> _logger;

    public LeavePolicyController(ILeavePolicyService service, ILogger<LeavePolicyController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Leave Types ───────────────────────────────────────────────────────────

    /// <summary>Returns all active leave types.</summary>
    [HttpGet("leave-types")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaveTypeDto>>>> GetAllLeaveTypes()
    {
        var types = await _service.GetAllLeaveTypesAsync();
        return Ok(ApiResponse<IEnumerable<LeaveTypeDto>>.Ok(types));
    }

    /// <summary>Returns a single leave type by id.</summary>
    [HttpGet("leave-types/{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> GetLeaveTypeById(Guid id)
    {
        var lt = await _service.GetLeaveTypeByIdAsync(id);
        if (lt is null)
            return NotFound(new ProblemDetails
            {
                Title = "Leave type not found",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_TYPE_NOT_FOUND" }
            });

        return Ok(ApiResponse<LeaveTypeDto>.Ok(lt));
    }

    /// <summary>Creates a new leave type. HRAdmin or SuperAdmin only.</summary>
    [HttpPost("leave-types")]
    [Authorize(Policy = "HRAdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> CreateLeaveType([FromBody] CreateLeaveTypeDto dto)
    {
        try
        {
            var created = await _service.CreateLeaveTypeAsync(dto);
            return CreatedAtAction(
                nameof(GetLeaveTypeById),
                new { id = created.Id },
                ApiResponse<LeaveTypeDto>.Ok(created));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("CreateLeaveType conflict: {Message}", ex.Message);
            return Conflict(new ProblemDetails
            {
                Title = "Leave type code conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["error_code"] = "LEAVE_TYPE_CODE_CONFLICT" }
            });
        }
    }

    /// <summary>Updates an existing leave type. HRAdmin or SuperAdmin only.</summary>
    [HttpPut("leave-types/{id:guid}")]
    [Authorize(Policy = "HRAdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> UpdateLeaveType(Guid id, [FromBody] UpdateLeaveTypeDto dto)
    {
        try
        {
            var updated = await _service.UpdateLeaveTypeAsync(id, dto);
            return Ok(ApiResponse<LeaveTypeDto>.Ok(updated));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("UpdateLeaveType not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Leave type not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_TYPE_NOT_FOUND" }
            });
        }
    }

    // ── Leave Policies ────────────────────────────────────────────────────────

    /// <summary>Returns all policies for a given leave type.</summary>
    [HttpGet("leave-types/{leaveTypeId:guid}/policies")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeavePolicyDto>>>> GetPoliciesForLeaveType(Guid leaveTypeId)
    {
        var policies = await _service.GetPoliciesForLeaveTypeAsync(leaveTypeId);
        return Ok(ApiResponse<IEnumerable<LeavePolicyDto>>.Ok(policies));
    }

    /// <summary>Returns the currently active policy for a given leave type.</summary>
    [HttpGet("leave-types/{leaveTypeId:guid}/policies/active")]
    public async Task<ActionResult<ApiResponse<LeavePolicyDto>>> GetActivePolicy(Guid leaveTypeId)
    {
        var policy = await _service.GetActivePolicyAsync(leaveTypeId);
        if (policy is null)
            return NotFound(new ProblemDetails
            {
                Title = "No active policy found",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "ACTIVE_POLICY_NOT_FOUND" }
            });

        return Ok(ApiResponse<LeavePolicyDto>.Ok(policy));
    }

    /// <summary>Creates a new leave policy. HRAdmin or SuperAdmin only.</summary>
    [HttpPost("leave-policies")]
    [Authorize(Policy = "HRAdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<LeavePolicyDto>>> CreatePolicy([FromBody] CreateLeavePolicyDto dto)
    {
        try
        {
            var created = await _service.CreatePolicyAsync(dto);
            return Created(string.Empty, ApiResponse<LeavePolicyDto>.Ok(created));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("CreatePolicy — leave type not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Leave type not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_TYPE_NOT_FOUND" }
            });
        }
    }

    /// <summary>Updates an existing leave policy. HRAdmin or SuperAdmin only.</summary>
    [HttpPut("leave-policies/{id:guid}")]
    [Authorize(Policy = "HRAdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<LeavePolicyDto>>> UpdatePolicy(Guid id, [FromBody] UpdateLeavePolicyDto dto)
    {
        try
        {
            var updated = await _service.UpdatePolicyAsync(id, dto);
            return Ok(ApiResponse<LeavePolicyDto>.Ok(updated));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("UpdatePolicy not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Leave policy not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "LEAVE_POLICY_NOT_FOUND" }
            });
        }
    }
}
