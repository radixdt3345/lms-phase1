namespace LMS.Application.DTOs.AuditLog;

/// <summary>
/// Paginated result returned by GET /api/audit-logs.
/// Mirrors the AuditLogPagedResult TypeScript type in the frontend.
/// </summary>
public class AuditLogPagedResultDto
{
    public IEnumerable<AuditLogDto> Items { get; set; } = Enumerable.Empty<AuditLogDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
