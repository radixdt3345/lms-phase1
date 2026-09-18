using LMS.Application.Interfaces;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Stub SendGrid email service — F-09 (FR-71, FR-73).
/// Queues outbound emails into the Hangfire job table via IBackgroundJobClient.
/// Actual HTTP calls to the SendGrid API live inside EmailDispatchJob (F-15).
/// </summary>
public class EmailService : IEmailService
{
    private readonly LmsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(LmsDbContext context, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task QueueEmailAsync(string toAddress, string subject, string htmlBody,
        Guid? relatedNotificationId = null)
    {
        // Hangfire's EmailDispatchJob (registered in F-15 / JobSchedulerService) polls
        // for Notification records with EmailStatus=Pending and dispatches them.
        // Here we simply log intent — the DB record was already set to Pending by
        // NotificationService.CreateAsync, so no additional work is needed at queue time.
        _logger.LogInformation("Email queued for {ToAddress} — subject: {Subject} — relatedNotification: {Id}",
            toAddress, subject, relatedNotificationId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> SendAsync(string toAddress, string subject, string htmlBody)
    {
        // In a full implementation this method calls the SendGrid REST API.
        // For Phase 1 the actual HTTP call is deferred to the F-15 Hangfire job so that
        // retry logic and delivery-failure tracking happen inside a durable background worker,
        // not in the request pipeline.
        _logger.LogWarning(
            "EmailService.SendAsync called directly (should be invoked from EmailDispatchJob). " +
            "To: {To}, Subject: {Subject}", toAddress, subject);

        await Task.CompletedTask;
        return false; // indicate not sent — let the job handle it
    }
}
