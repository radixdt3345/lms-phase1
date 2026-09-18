using LMS.Application.DTOs.CompOff;

namespace LMS.Application.Interfaces;

/// <summary>
/// Comp-Off Management service — F-07 API layer (FR-58 to FR-65).
/// </summary>
public interface ICompOffService
{
    /// <summary>
    /// Submits a new comp-off request for the given employee.
    /// Validates that DateWorked is a public holiday or weekend (FR-59).
    /// Validates that worked hours meet the minimum 4h threshold (FR-60).
    /// Calculates credit: 0.5 for [4h, 8h), 1.0 for >= 8h (FR-60).
    /// Throws InvalidOperationException with code NOT_A_NON_WORKING_DAY or INSUFFICIENT_HOURS.
    /// </summary>
    Task<CompOffRequestDto> SubmitAsync(Guid employeeId, CreateCompOffRequestDto dto);

    /// <summary>
    /// Returns comp-off requests scoped to the caller's role:
    /// - Employee: own requests only.
    /// - Manager: requests from direct reports.
    /// - HRAdmin/SuperAdmin: all requests.
    /// </summary>
    Task<IEnumerable<CompOffRequestDto>> GetListAsync(Guid callerId, string callerRole);

    /// <summary>Returns a single comp-off request by ID, or null if not found.</summary>
    Task<CompOffRequestDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Approves a pending comp-off request (Manager or HRAdmin only — FR-61).
    /// Credits the calculated amount to the employee's Comp-off LeaveBalance (FR-62).
    /// Creates a CompOffCredit record with expiry = earned_date + 30 days (FR-62).
    /// Throws InvalidOperationException if request is not in Pending status.
    /// Throws UnauthorizedAccessException if caller is not the assigned approver or HRAdmin.
    /// </summary>
    Task<CompOffRequestDto> ApproveAsync(Guid requestId, Guid approverId, string approverRole);

    /// <summary>
    /// Rejects a pending comp-off request with a mandatory reason (FR-61).
    /// Throws InvalidOperationException if request is not in Pending status.
    /// Throws UnauthorizedAccessException if the caller is the request owner (AC-55).
    /// </summary>
    Task<CompOffRequestDto> RejectAsync(Guid requestId, Guid callerId, string callerRole, string rejectionReason);

    /// <summary>
    /// Returns the comp-off credit balance for the given employee.
    /// HRAdmin/Manager may query any employee; Employee may only query themselves.
    /// </summary>
    Task<CompOffCreditDto> GetCreditsAsync(Guid employeeId);
}
