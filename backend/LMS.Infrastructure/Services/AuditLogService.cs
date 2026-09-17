using LMS.Application.DTOs.AuditLog;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements audit log querying and appending (F-13).
/// The table is append-only — no update or delete operations are exposed.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(LmsDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<AuditLogPagedResultDto> GetPagedAsync(AuditLogQueryParams query)
    {
        // Clamp page size to prevent abusive queries
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(1, query.Page);

        var q = _context.AuditLogs.AsNoTracking();

        if (query.UserId.HasValue)
            q = q.Where(a => a.ActorUserId == query.UserId.Value);

        if (!string.IsNullOrWhiteSpace(query.ActionType))
            q = q.Where(a => a.ActionType.ToLower() == query.ActionType.ToLower());

        if (!string.IsNullOrWhiteSpace(query.RecordType))
            q = q.Where(a => a.RecordType.ToLower() == query.RecordType.ToLower());

        if (query.DateFrom.HasValue)
            q = q.Where(a => a.Timestamp >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
        {
            // Include the full day when only a date (no time) is supplied
            var upperBound = query.DateTo.Value.TimeOfDay == TimeSpan.Zero
                ? query.DateTo.Value.AddDays(1)
                : query.DateTo.Value;
            q = q.Where(a => a.Timestamp < upperBound);
        }

        var totalCount = await q.CountAsync();

        var items = await q
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                ActorEmail = a.ActorEmail,
                ActionType = a.ActionType,
                RecordType = a.RecordType,
                RecordId = a.RecordId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp,
            })
            .ToListAsync();

        return new AuditLogPagedResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    // ── Write (called only from AuditInterceptor) ──────────────────────────────

    public async Task LogAsync(
        Guid actorUserId,
        string actorEmail,
        string actionType,
        string recordType,
        string recordId,
        string? oldValue,
        string? newValue,
        string ipAddress)
    {
        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            ActionType = actionType,
            RecordType = recordType,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow,
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "AuditLog: {ActionType} on {RecordType}/{RecordId} by {ActorEmail}",
            actionType, recordType, recordId, actorEmail);
    }
}
