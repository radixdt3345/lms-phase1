using LMS.Application.DTOs.Notification;

namespace LMS.Application.Interfaces;

/// <summary>
/// In-app notification operations — F-09 API layer (FR-72 to FR-75).
/// </summary>
public interface INotificationService
{
    /// <summary>Returns paginated notifications for the current user (newest first).</summary>
    Task<NotificationPageDto> GetMyNotificationsAsync(Guid userId, int page, int pageSize);

    /// <summary>Returns the count of unread notifications for the current user.</summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>Marks a single notification as read. Returns false if not found or not owned by user.</summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>Marks all notifications for the current user as read.</summary>
    Task<bool> MarkAllAsReadAsync(Guid userId);

    /// <summary>Soft-deletes a notification. Returns false if not found or not owned by user.</summary>
    Task<bool> DeleteAsync(Guid notificationId, Guid userId);

    /// <summary>Creates a new in-app notification (called by other services).</summary>
    Task<NotificationDto> CreateAsync(Guid recipientUserId, string type, string title, string message,
        Guid? relatedEntityId = null, string? relatedEntityType = null);
}
