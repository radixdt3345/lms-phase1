namespace LMS.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string EmailStatus { get; set; } = NotificationEmailStatus.NotSent;
    public int EmailRetryCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User RecipientUser { get; set; } = null!;
}

public static class NotificationEmailStatus
{
    public const string NotSent = "not_sent";
    public const string Sent = "sent";
    public const string DeliveryFailed = "DELIVERY_FAILED";
    public const string Pending = "pending";
}
