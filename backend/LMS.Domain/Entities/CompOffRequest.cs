using System;

namespace LMS.Domain.Entities;

/// <summary>
/// Records a request submitted by an employee who worked on a public holiday or weekend
/// to earn compensatory leave credit.
/// Credit thresholds: worked hours >= 4h earns 0.5 days (half-day); >= 8h earns 1.0 day.
/// Requests with fewer than 4 worked hours are blocked at the application layer.
/// </summary>
public class CompOffRequest
{
    public Guid Id { get; set; }

    /// <summary>The employee (user) who worked and is claiming the comp-off.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>The date on which the employee worked (must be a public holiday or weekend).</summary>
    public DateOnly DateWorked { get; set; }

    /// <summary>Shift start time on the worked date.</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Shift end time on the worked date.</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>True when the credit to be earned is a half-day (0.5) rather than a full day (1.0).</summary>
    public bool IsHalfDay { get; set; }

    /// <summary>Employee-supplied description of work performed.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Credit value calculated from worked hours: 0.5 or 1.0.</summary>
    public decimal CalculatedCredit { get; set; }

    /// <summary>
    /// Approval status: Pending, Approved, Rejected.
    /// Note: Cancelled is not a valid status — employees cannot cancel their own request;
    /// they must ask the approver to reject it.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>The manager (user) responsible for approving this request. Null until routed.</summary>
    public Guid? ApproverId { get; set; }

    /// <summary>Timestamp when the request was approved or rejected.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Mandatory when Status = Rejected; written by the approver.</summary>
    public string? RejectionReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public User Employee { get; set; } = null!;
    public User? Approver { get; set; }
}
