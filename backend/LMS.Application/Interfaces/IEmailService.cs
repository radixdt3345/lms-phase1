namespace LMS.Application.Interfaces;

/// <summary>
/// Email delivery abstraction — F-09 (FR-71, FR-73).
/// Actual SendGrid integration is dispatched via Hangfire (EmailDispatchJob in F-15).
/// This interface allows services to queue emails without coupling to SendGrid directly.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Queues an email notification for delivery via Hangfire background job.
    /// The job will attempt delivery up to 5 times over 24 hours.
    /// On final failure the Notification record is updated to email_status=DELIVERY_FAILED.
    /// </summary>
    Task QueueEmailAsync(string toAddress, string subject, string htmlBody,
        Guid? relatedNotificationId = null);

    /// <summary>
    /// Attempts immediate delivery of an email via SendGrid.
    /// Returns true if accepted (2xx), false on 5xx (caller should re-queue).
    /// </summary>
    Task<bool> SendAsync(string toAddress, string subject, string htmlBody);
}
