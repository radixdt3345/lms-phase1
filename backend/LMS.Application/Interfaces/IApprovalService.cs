using LMS.Application.DTOs.Approval;

namespace LMS.Application.Interfaces;

/// <summary>
/// Approval Workflow service — F-08 API layer (FR-66 to FR-69).
/// Manages the two-level approval chain for leave and comp-off requests.
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Returns pending approval actions for the current manager/HR Admin (FR-68).
    /// - Manager: sees L1 actions where they are the assigned approver and action is still null.
    /// - HRAdmin: sees all L2 actions that are still pending.
    /// </summary>
    Task<IEnumerable<ApprovalRecordDto>> GetPendingAsync(Guid callerId, string callerRole);

    /// <summary>
    /// Returns the approval history for the caller:
    /// - Manager: records where they acted (action is not null).
    /// - HRAdmin: all records with a non-null action.
    /// </summary>
    Task<IEnumerable<ApprovalRecordDto>> GetHistoryAsync(Guid callerId, string callerRole);

    /// <summary>
    /// Escalates a leave request to HR Admin after L1 approval (Manager only — FR-67).
    /// Transitions the leave request status to WaitingL2 / WAITING_HR_APPROVAL.
    /// Creates an L2 ApprovalRecord for HR Admin.
    /// Throws InvalidOperationException if the request is not in a state that allows escalation.
    /// Throws UnauthorizedAccessException if the caller is not the L1 approver.
    /// </summary>
    Task<ApprovalRecordDto> EscalateAsync(Guid leaveRequestId, Guid managerId, string reason);

    /// <summary>
    /// Returns approval stats for the dashboard (FR-68 supporting data).
    /// </summary>
    Task<ApprovalStatsDto> GetStatsAsync(Guid callerId, string callerRole);
}
