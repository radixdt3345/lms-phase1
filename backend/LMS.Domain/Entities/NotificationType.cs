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

// NotificationEmailStatus is defined in Notification.cs (same namespace).
// Do not redefine here — CS0101 duplicate type.
