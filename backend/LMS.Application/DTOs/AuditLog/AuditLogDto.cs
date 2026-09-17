namespace LMS.Application.DTOs.AuditLog;

/// <summary>
/// Read-only projection of an AuditLog row returned by GET /api/audit-logs.
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
