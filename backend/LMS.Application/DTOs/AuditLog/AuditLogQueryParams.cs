namespace LMS.Application.DTOs.AuditLog;

/// <summary>
/// Query parameters for GET /api/audit-logs — bound from query string.
/// All filter fields are optional; page/pageSize always required.
/// </summary>
public class AuditLogQueryParams
{
    /// <summary>Filter by actor user ID (exact match).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Filter by action type string (case-insensitive), e.g. "CREATE", "UPDATE".</summary>
    public string? ActionType { get; set; }

    /// <summary>Filter by record type string (case-insensitive), e.g. "Employee", "Department".</summary>
    public string? RecordType { get; set; }

    /// <summary>Inclusive lower bound of the timestamp range (ISO 8601 date string).</summary>
    public DateTimeOffset? DateFrom { get; set; }

    /// <summary>Inclusive upper bound of the timestamp range (ISO 8601 date string).</summary>
    public DateTimeOffset? DateTo { get; set; }

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Page size. Defaults to 50, capped at 200.</summary>
    public int PageSize { get; set; } = 50;
}
