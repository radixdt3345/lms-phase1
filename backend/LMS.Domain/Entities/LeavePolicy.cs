namespace LMS.Domain.Entities;

/// <summary>
/// Configures accrual, carry-forward, and notice rules for a specific LeaveType.
/// Each LeaveType may have multiple policies (e.g. one per role group, or over time).
/// </summary>
public class LeavePolicy
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid LeaveTypeId { get; set; }

    /// <summary>
    /// Optional role restriction — null means the policy applies to all employees.
    /// Stored as a plain string so the Policy domain is not coupled to the Role table FK
    /// (roles are defined in the Auth domain; cross-domain FK enforced at app layer).
    /// </summary>
    public string? ApplicableToRole { get; set; }

    /// <summary>Days granted per leave year. Copied from LeaveType.AnnualDays at policy creation
    /// but may be overridden per policy (e.g. different entitlement for senior staff).</summary>
    public int AnnualAllotment { get; set; }

    /// <summary>Maximum days that can be carried forward to the next leave year. 0 = no carry-forward.</summary>
    public int MaxCarryForward { get; set; }

    /// <summary>Maximum consecutive days that may be taken in a single request.</summary>
    public int MaxConsecutiveDays { get; set; } = 30;

    /// <summary>Minimum advance notice days required when submitting the leave request.</summary>
    public int MinNoticeDays { get; set; }

    /// <summary>When true, leave is accrued monthly (AccrualRate days/month) rather than credited at year start.</summary>
    public bool AccrualMonthly { get; set; }

    /// <summary>Days accrued per calendar month when AccrualMonthly is true. Null when accrual is not monthly.</summary>
    public decimal? AccrualRate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public LeaveType LeaveType { get; set; } = null!;
}
