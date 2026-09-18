using System;

namespace LMS.Domain.Entities;

/// <summary>
/// Tracks each employee's leave balance per leave type per calendar year.
/// Balances are credited on January 1, updated on every approval/cancellation/revocation,
/// and zeroed on December 31 via a scheduled job.
/// </summary>
public class LeaveBalance
{
    public Guid Id { get; set; }

    /// <summary>The employee (user) this balance belongs to.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>The type of leave this balance tracks.</summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>Calendar year for which this balance is valid (e.g. 2026).</summary>
    public int Year { get; set; }

    /// <summary>Total days allocated for the year (after pro-ration for mid-year joiners).</summary>
    public decimal TotalDays { get; set; }

    /// <summary>Days consumed by approved leave requests.</summary>
    public decimal UsedDays { get; set; }

    /// <summary>Days held against pending (submitted/in-approval) leave requests.</summary>
    public decimal PendingDays { get; set; }

    /// <summary>Manual adjustments applied by HR Admin (positive or negative).</summary>
    public decimal AdjustedDays { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public User Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
