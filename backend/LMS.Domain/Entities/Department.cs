namespace LMS.Domain.Entities;

/// <summary>
/// Flat department entity (FR-19, FR-20). No hierarchy — departments are a flat list.
/// Soft-deleted via DeletedAt (FR-23). Overlap limit drives leave concurrency enforcement (FR-26).
/// </summary>
public class Department
{
    public Guid Id { get; set; }

    /// <summary>Full department name, unique across active departments (case-insensitive) — FR-25.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code, e.g. "HR", "ENG" — unique across active departments (case-insensitive) — FR-25.</summary>
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Maximum number of employees who can be on leave simultaneously in this department — FR-26.
    /// </summary>
    public int OverlapLimit { get; set; } = 1;

    /// <summary>Whether the department is active. Deactivating retains data (FR-23).</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Soft-delete marker — null means active, non-null means deactivated (FR-23).</summary>
    public DateTime? DeletedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
