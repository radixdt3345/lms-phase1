namespace LMS.Domain.Entities;

/// <summary>
/// Immutable audit log record. Every mutating API action (CREATE/UPDATE/DELETE) writes one row.
/// Rows are never updated or deleted by any role — the table is append-only (FR-F13-01).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>User ID of the actor who performed the action.</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>Email address of the actor at the time of the action.</summary>
    public string ActorEmail { get; set; } = string.Empty;

    /// <summary>Action performed: CREATE, UPDATE, DELETE, APPROVE, REJECT, LOGIN, LOGOUT, etc.</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>Entity / record type affected, e.g. "Employee", "Department", "LeaveType".</summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>Primary key of the affected record (string so it works for any key type).</summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the record before the action (null for CREATE).</summary>
    public string? OldValue { get; set; }

    /// <summary>JSON snapshot of the record after the action (null for DELETE).</summary>
    public string? NewValue { get; set; }

    /// <summary>IP address of the client making the request.</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the action occurred.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
