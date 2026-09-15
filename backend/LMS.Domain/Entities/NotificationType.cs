namespace LMS.Domain.Entities;

public static class NotificationType
{
    public const string LeaveApproved = "leave_approved";
    public const string LeaveRejected = "leave_rejected";
    public const string LeaveSubmitted = "leave_submitted";
    public const string LeaveEscalated = "leave_escalated";
    public const string LeaveCancelled = "leave_cancelled";
    public const string LeaveRevoked = "leave_revoked";
    public const string ApprovalReminder = "approval_reminder";
    public const string CompOffSubmitted = "comp_off_submitted";
    public const string CompOffApproved = "comp_off_approved";
    public const string CompOffRejected = "comp_off_rejected";
    public const string BalanceLow = "balance_low";
    public const string HolidayAdded = "holiday_added";
}
