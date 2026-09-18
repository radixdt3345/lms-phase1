using System;

namespace LMS.Domain.Entities;

/// <summary>
/// Records an individual comp-off credit earned by an employee for working on a public holiday
/// or weekend. Each credit carries a 30-day expiry window and is consumed when the employee
/// takes comp-off leave.
/// </summary>
public class CompOffCredit
{
    public Guid Id { get; set; }

    /// <summary>The employee (user) who earned this credit.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>The date the credit was earned (the date the employee worked).</summary>
    public DateOnly EarnedDate { get; set; }

    /// <summary>Expiry date — exactly 30 days after EarnedDate. Credits not consumed by this date become Expired.</summary>
    public DateOnly ExpiryDate { get; set; }

    /// <summary>Credit value: 0.5 for a half-day, 1.0 for a full day.</summary>
    public decimal Days { get; set; }

    /// <summary>Current status: Active, Used, or Expired.</summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// The leave request that consumed this credit (set when Status = Used).
    /// FK to leave_requests; enforced after the leave_requests table is created (F-06 migration).
    /// </summary>
    public Guid? LeaveRequestId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public User Employee { get; set; } = null!;
}
