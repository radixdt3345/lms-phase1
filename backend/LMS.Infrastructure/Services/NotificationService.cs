using LMS.Application.DTOs.Notification;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// In-app notification service — F-09 API layer (FR-72 to FR-75).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(LmsDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<NotificationPageDto> GetMyNotificationsAsync(Guid userId, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new NotificationPageDto
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);

        if (notification is null)
            return false;

        if (notification.IsRead)
            return true; // already read — idempotent

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0)
            return true;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
            n.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unread.Count, userId);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);

        if (notification is null)
            return false;

        // Hard delete — Notification entity has no DeletedAt; remove the record.
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}", notificationId, userId);
        return true;
    }

    public async Task<NotificationDto> CreateAsync(Guid recipientUserId, string type, string title,
        string message, Guid? relatedEntityId = null, string? relatedEntityType = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            EmailStatus = NotificationEmailStatus.Pending,
            EmailRetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created notification {NotificationId} for user {UserId}", notification.Id, recipientUserId);
        return MapToDto(notification);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        RelatedEntityId = n.RelatedEntityId,
        RelatedEntityType = n.RelatedEntityType,
        EmailStatus = n.EmailStatus,
        CreatedAt = n.CreatedAt
    };
}
