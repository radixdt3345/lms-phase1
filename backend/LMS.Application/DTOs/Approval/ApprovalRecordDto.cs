namespace LMS.Application.DTOs.Approval;

/// <summary>
/// Read model for a single approval record — returned by pending and history endpoints.
/// </summary>
public class ApprovalRecordDto
{
    public Guid Id { get; set; }

    /// <summary>The leave request this approval is associated with.</summary>
    public Guid LeaveRequestId { get; set; }

    /// <summary>The comp-off request this approval is associated with (null for leave requests).</summary>
    public Guid? CompOffRequestId { get; set; }

    /// <summary>The user who acted (or needs to act) on this approval.</summary>
    public Guid ApproverId { get; set; }
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>The employee who submitted the original request.</summary>
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Approval chain level: L1 (manager) or L2 (HR Admin).</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Decision taken: APPROVED, REJECTED, or null if still pending.</summary>
    public string? Action { get; set; }

    /// <summary>Optional comments from the approver.</summary>
    public string? Comments { get; set; }

    /// <summary>Timestamp when the approver acted. Null = still pending.</summary>
    public DateTime? ActedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Human-readable summary of the underlying request (dates, type, days).</summary>
    public string RequestSummary { get; set; } = string.Empty;

    /// <summary>Current status of the underlying leave/comp-off request.</summary>
    public string RequestStatus { get; set; } = string.Empty;
}
