namespace LMS.Domain.Entities;

/// <summary>
/// Records each approval action taken on a leave or comp-off request.
/// Supports the two-level approval chain (L1 = reporting manager, L2 = HR Admin).
/// </summary>
public class ApprovalRecord
{
    public Guid Id { get; set; }

    /// <summary>FK to leave_requests table (created by F-07 DB migration).</summary>
    public Guid LeaveRequestId { get; set; }

    /// <summary>FK to comp_off_requests table — nullable because not all approvals are for comp-off.</summary>
    public Guid? CompOffRequestId { get; set; }

    /// <summary>FK to users table — the employee who took the approval action.</summary>
    public Guid ApproverId { get; set; }

    /// <summary>Approval chain level: L1 (reporting manager) or L2 (HR Admin).</summary>
    public string Level { get; set; } = ApprovalLevel.L1;

    /// <summary>Decision taken by the approver.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Optional justification or remarks from the approver.</summary>
    public string? Comments { get; set; }

    /// <summary>Timestamp when the approver took action (null if still pending).</summary>
    public DateTime? ActedAt { get; set; }

    /// <summary>Timestamp of the last reminder email sent to the approver.</summary>
    public DateTime? ReminderSentAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public User Approver { get; set; } = null!;
}

/// <summary>Approval chain levels for the two-level approval workflow.</summary>
public static class ApprovalLevel
{
    public const string L1 = "L1";
    public const string L2 = "L2";
}

/// <summary>Possible actions an approver can take on a leave/comp-off request.</summary>
public static class ApprovalAction
{
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}
