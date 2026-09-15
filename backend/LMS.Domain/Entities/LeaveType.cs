namespace LMS.Domain.Entities;

/// <summary>
/// Represents a category of leave (e.g., Casual Leave, Sick Leave, Earned Leave).
/// Drives the leave application form dropdown and balance credit rules.
/// </summary>
public class LeaveType
{
    public Guid Id { get; set; }

    /// <summary>Full display name — e.g. "Casual Leave".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code — e.g. "CL", "SL". Must be unique.</summary>
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Total days granted per leave year (Jan 1 – Dec 31). 0 for Comp-off and Unpaid.</summary>
    public int AnnualDays { get; set; }

    /// <summary>When true, employee must attach a document (e.g. medical certificate) with the request.</summary>
    public bool RequiresAttachment { get; set; }

    /// <summary>When true, HR must approve in addition to the line manager.</summary>
    public bool RequiresHrApproval { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp — null means the record is live.</summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<LeavePolicy> LeavePolicies { get; set; } = new List<LeavePolicy>();
}
