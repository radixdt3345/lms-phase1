using LMS.Application.DTOs.AuditLog;

namespace LMS.Application.Interfaces;

/// <summary>
/// Read-only service for querying immutable audit log entries (F-13).
/// Writing audit entries is handled by AuditInterceptor — consumers never call a write method.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Returns a paginated, filtered list of audit log entries ordered by timestamp descending.
    /// </summary>
    Task<AuditLogPagedResultDto> GetPagedAsync(AuditLogQueryParams query);

    /// <summary>
    /// Appends a new audit log entry. Called only by AuditInterceptor / integration layer.
    /// </summary>
    Task LogAsync(
        Guid actorUserId,
        string actorEmail,
        string actionType,
        string recordType,
        string recordId,
        string? oldValue,
        string? newValue,
        string ipAddress);
}
