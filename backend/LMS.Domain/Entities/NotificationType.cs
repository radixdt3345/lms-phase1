namespace LMS.Domain.Entities;

/// <summary>
/// Notification type constants for F-09 Notifications and Email feature.
/// Maps to the email_status and notification type values stored in the notifications table.
/// </summary>
public static class NotificationType
{
    // Leave lifecycle events
    public const string LeaveApproved = "leave_approved";
    public const string LeaveRejected = "leave_rejected";
    public const string LeaveSubmitted = "leave_submitted";
    public const string LeaveEscalated = "leave_escalated";
    public const string LeaveCancelled = "leave_cancelled";
    public const string LeaveRevoked = "leave_revoked";

    // Approval reminder
    public const string ApprovalReminder = "approval_reminder";

    // Comp-off lifecycle events
    public const string CompOffSubmitted = "comp_off_submitted";
    public const string CompOffApproved = "comp_off_approved";
    public const string CompOffRejected = "comp_off_rejected";

    // Balance and holiday alerts
    public const string BalanceLow = "balance_low";
    public const string HolidayAdded = "holiday_added";

    // System-level alerts (F-09)
    public const string SystemAlert = "system_alert";
}

/// <summary>
/// Email delivery status constants for the notifications.email_status column.
/// These values match the CHECK constraint in migration 20260918040000_UpdateNotificationsEmailStatus.
/// </summary>
public static class NotificationEmailStatus
{
    /// <summary>Email is queued for delivery via SendGrid.</summary>
    public const string Queued = "QUEUED";

    /// <summary>Email was successfully delivered.</summary>
    public const string Sent = "SENT";

    /// <summary>All delivery retries exhausted — email could not be delivered.</summary>
    public const string DeliveryFailed = "DELIVERY_FAILED";

    // Legacy values kept for backward compatibility with existing rows
    public const string NotSent = "not_sent";
    public const string Pending = "pending";
}
