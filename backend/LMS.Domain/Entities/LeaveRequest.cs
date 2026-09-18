using System;

namespace LMS.Domain.Entities;

/// <summary>
/// Central entity for the leave request lifecycle: Draft → Submitted → Waiting L1 →
/// Manager Approved → Waiting L2 → Fully Approved → Active → Completed.
/// Terminal states: Rejected, Cancelled, Revoked.
/// </summary>
public class LeaveRequest
{
    public Guid Id { get; set; }

    /// <summary>The employee (user) who raised this request.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Type of leave being requested.</summary>
    public Guid LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>When true this request covers only half a day.</summary>
    public bool IsHalfDay { get; set; }

    /// <summary>For half-day requests: "Morning" or "Afternoon".</summary>
    public string? HalfDayPeriod { get; set; }

    /// <summary>Employee-supplied reason for the leave.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle status. Valid values: Draft, Submitted, WaitingL1,
    /// ManagerApproved, WaitingL2, FullyApproved, Active, Completed,
    /// Rejected, Cancelled, Revoked.
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Number of working days calculated after applying sandwich rule, half-day flag,
    /// and excluding weekends/public holidays.
    /// </summary>
    public decimal CalculatedDays { get; set; }

    /// <summary>Reference to the uploaded attachment (nullable — attachments are optional).</summary>
    public Guid? AttachmentId { get; set; }

    /// <summary>Timestamp when the employee formally submitted the request (moves out of Draft).</summary>
    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public User Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public LeaveRequestAttachment? Attachment { get; set; }
    public ICollection<CompOffCredit> CompOffCredits { get; set; } = new List<CompOffCredit>();
}
