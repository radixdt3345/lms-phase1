using LMS.Application.DTOs.LeaveRequest;

namespace LMS.Application.Interfaces;

/// <summary>
/// Leave Application and Workflow operations — F-06 API layer (FR-42 to FR-57, FR-70).
/// Covers the full leave request lifecycle from Draft to terminal states.
/// </summary>
public interface ILeaveRequestService
{
    /// <summary>
    /// Creates a new leave request. When <paramref name="dto"/>.SaveAsDraft is true,
    /// the request is stored as Draft (no notification, no balance deduction — FR-43).
    /// When false, the request is submitted immediately (status → Submitted).
    /// Throws <see cref="InvalidOperationException"/> on validation failures
    /// (insufficient balance, overlap, team limit exceeded, etc.).
    /// </summary>
    Task<LeaveRequestDto> CreateAsync(Guid employeeId, CreateLeaveRequestDto dto);

    /// <summary>
    /// Returns leave requests scoped by the caller's role:
    /// Employee → own requests only, Manager → team requests, HRAdmin → all.
    /// </summary>
    Task<IEnumerable<LeaveRequestDto>> GetAllAsync(Guid requesterId, IEnumerable<string> roles);

    /// <summary>Returns a single leave request by ID, or null if not found.</summary>
    Task<LeaveRequestDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Cancels a pending request (status: Submitted or WaitingL1) raised by
    /// <paramref name="employeeId"/>. Restores the full balance immediately (FR-51).
    /// Throws <see cref="KeyNotFoundException"/> when not found.
    /// Throws <see cref="InvalidOperationException"/> when the request is not in a
    /// cancellable state or does not belong to the employee.
    /// </summary>
    Task<LeaveRequestDto> CancelAsync(Guid id, Guid employeeId);

    /// <summary>
    /// Approves a leave request. Advances status through the state machine
    /// (WaitingL1 → ManagerApproved → FullyApproved or WaitingL2 depending on policy — FR-48).
    /// Throws <see cref="KeyNotFoundException"/> when not found.
    /// Throws <see cref="InvalidOperationException"/> when the request is not in an approvable state.
    /// </summary>
    Task<LeaveRequestDto> ApproveAsync(Guid id, Guid approverId);

    /// <summary>
    /// Rejects a leave request (mandatory reason — AC-44).
    /// Throws <see cref="KeyNotFoundException"/> when not found.
    /// Throws <see cref="InvalidOperationException"/> when the request is not in a rejectable state.
    /// </summary>
    Task<LeaveRequestDto> RejectAsync(Guid id, Guid approverId, string reason);

    /// <summary>
    /// Returns all leave requests currently pending approval by the given approver
    /// (status: Submitted, WaitingL1, or WaitingL2).
    /// </summary>
    Task<IEnumerable<LeaveRequestDto>> GetPendingForApproverAsync(Guid approverId);
}
