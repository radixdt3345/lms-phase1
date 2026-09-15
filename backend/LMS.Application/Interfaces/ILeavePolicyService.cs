using LMS.Application.DTOs.LeavePolicy;

namespace LMS.Application.Interfaces;

public interface ILeavePolicyService
{
    // ── Leave Types ───────────────────────────────────────────────────────────

    Task<IEnumerable<LeaveTypeDto>> GetAllLeaveTypesAsync();

    Task<LeaveTypeDto?> GetLeaveTypeByIdAsync(Guid id);

    Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeDto dto);

    Task<LeaveTypeDto> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeDto dto);

    // ── Leave Policies ────────────────────────────────────────────────────────

    Task<IEnumerable<LeavePolicyDto>> GetPoliciesForLeaveTypeAsync(Guid leaveTypeId);

    /// <summary>Returns the currently active policy — one with EffectiveTo null or in the future.</summary>
    Task<LeavePolicyDto?> GetActivePolicyAsync(Guid leaveTypeId);

    Task<LeavePolicyDto> CreatePolicyAsync(CreateLeavePolicyDto dto);

    Task<LeavePolicyDto> UpdatePolicyAsync(Guid id, UpdateLeavePolicyDto dto);
}
