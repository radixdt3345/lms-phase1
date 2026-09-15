using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F10-DB-001 — Notification and Alerts DB schema.
/// Covers entity properties, constants, DbContext, query patterns, and FK relationships.
/// </summary>
public class NotificationDbTests
{
    private static LmsDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    private static User CreateTestUser(Guid? id = null) => new User
    {
        Id = id ?? Guid.NewGuid(),
        AzureAdObjectId = $"aad-{Guid.NewGuid()}",
        Email = $"user-{Guid.NewGuid()}@test.com",
        DisplayName = "Test User",
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // UT-NOTIFY-001: Notification entity has all required properties with correct defaults
    [Fact]
    public void Notification_Entity_HasRequiredPropertiesAndDefaults()
    {
        var recipientId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientId,
            Type = NotificationType.LeaveApproved,
            Title = "Leave Approved",
            Message = "Your leave request has been approved.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        notification.Id.Should().NotBeEmpty();
        notification.RecipientUserId.Should().Be(recipientId);
        notification.Type.Should().Be(NotificationType.LeaveApproved);
        notification.Title.Should().Be("Leave Approved");
        notification.Message.Should().Be("Your leave request has been approved.");
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.RelatedEntityId.Should().BeNull();
        notification.RelatedEntityType.Should().BeNull();
        notification.EmailStatus.Should().Be(NotificationEmailStatus.NotSent);
        notification.EmailRetryCount.Should().Be(0);
    }

    // UT-NOTIFY-002: NotificationType constants are all non-empty strings with correct values
    [Fact]
    public void NotificationType_Constants_AreNonEmptyStringsWithCorrectValues()
    {
        NotificationType.LeaveApproved.Should().Be("leave_approved").And.NotBeEmpty();
        NotificationType.LeaveRejected.Should().Be("leave_rejected").And.NotBeEmpty();
        NotificationType.LeaveSubmitted.Should().Be("leave_submitted").And.NotBeEmpty();
        NotificationType.LeaveEscalated.Should().Be("leave_escalated").And.NotBeEmpty();
        NotificationType.LeaveCancelled.Should().Be("leave_cancelled").And.NotBeEmpty();
        NotificationType.LeaveRevoked.Should().Be("leave_revoked").And.NotBeEmpty();
        NotificationType.ApprovalReminder.Should().Be("approval_reminder").And.NotBeEmpty();
        NotificationType.CompOffSubmitted.Should().Be("comp_off_submitted").And.NotBeEmpty();
        NotificationType.CompOffApproved.Should().Be("comp_off_approved").And.NotBeEmpty();
        NotificationType.CompOffRejected.Should().Be("comp_off_rejected").And.NotBeEmpty();
        NotificationType.BalanceLow.Should().Be("balance_low").And.NotBeEmpty();
        NotificationType.HolidayAdded.Should().Be("holiday_added").And.NotBeEmpty();
    }

    // UT-NOTIFY-003: NotificationEmailStatus constants are non-empty and semantically correct
    [Fact]
    public void NotificationEmailStatus_Constants_AreNonEmptyAndCorrect()
    {
        NotificationEmailStatus.NotSent.Should().Be("not_sent").And.NotBeEmpty();
        NotificationEmailStatus.Sent.Should().Be("sent").And.NotBeEmpty();
        NotificationEmailStatus.DeliveryFailed.Should().Be("DELIVERY_FAILED").And.NotBeEmpty();
        NotificationEmailStatus.Pending.Should().Be("pending").And.NotBeEmpty();
    }

    // UT-NOTIFY-004: LmsDbContext includes Notifications DbSet
    [Fact]
    public void LmsDbContext_IncludesNotificationsDbSet()
    {
        using var context = CreateInMemoryContext(nameof(LmsDbContext_IncludesNotificationsDbSet));

        context.Notifications.Should().NotBeNull();

        // Verify the DbSet is correctly typed and queryable
        var queryable = context.Notifications.AsQueryable();
        queryable.Should().NotBeNull();
    }

    // UT-NOTIFY-005: Unread notifications query filters correctly by IsRead = false
    [Fact]
    public async Task Notifications_UnreadQuery_FiltersCorrectlyByIsRead()
    {
        using var context = CreateInMemoryContext(nameof(Notifications_UnreadQuery_FiltersCorrectlyByIsRead));

        var user = CreateTestUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var readNotification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.LeaveApproved,
            Title = "Read Notification",
            Message = "This one is read.",
            IsRead = true,
            ReadAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var unreadNotification1 = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.LeaveRejected,
            Title = "Unread 1",
            Message = "First unread notification.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        var unreadNotification2 = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.CompOffApproved,
            Title = "Unread 2",
            Message = "Second unread notification.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        context.Notifications.AddRange(readNotification, unreadNotification1, unreadNotification2);
        await context.SaveChangesAsync();

        // Query unread count — same pattern as the API will use
        var unreadCount = await context.Notifications
            .Where(n => n.RecipientUserId == user.Id && !n.IsRead)
            .CountAsync();

        var unreadList = await context.Notifications
            .Where(n => n.RecipientUserId == user.Id && !n.IsRead)
            .ToListAsync();

        unreadCount.Should().Be(2);
        unreadList.Should().HaveCount(2);
        unreadList.All(n => !n.IsRead).Should().BeTrue();
    }

    // UT-NOTIFY-006: FK relationship — Notification links to User via RecipientUserId
    [Fact]
    public async Task Notification_FkRelationship_LinksToUserViaRecipientUserId()
    {
        using var context = CreateInMemoryContext(nameof(Notification_FkRelationship_LinksToUserViaRecipientUserId));

        var user = CreateTestUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.LeaveSubmitted,
            Title = "Leave Submitted",
            Message = "A new leave request has been submitted.",
            RelatedEntityId = Guid.NewGuid(),
            RelatedEntityType = "LeaveRequest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var saved = await context.Notifications
            .Include(n => n.RecipientUser)
            .FirstAsync(n => n.Id == notification.Id);

        saved.RecipientUserId.Should().Be(user.Id);
        saved.RecipientUser.Should().NotBeNull();
        saved.RecipientUser.Email.Should().Be(user.Email);
        saved.RelatedEntityType.Should().Be("LeaveRequest");
        saved.RelatedEntityId.Should().NotBeNull();
    }

    // UT-NOTIFY-007: Mark as read — sets IsRead and ReadAt correctly
    [Fact]
    public async Task Notification_MarkAsRead_SetsIsReadAndReadAt()
    {
        using var context = CreateInMemoryContext(nameof(Notification_MarkAsRead_SetsIsReadAndReadAt));

        var user = CreateTestUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.LeaveApproved,
            Title = "Leave Approved",
            Message = "Your request was approved.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Simulate mark-as-read
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var updated = await context.Notifications.FirstAsync(n => n.Id == notification.Id);
        updated.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    // UT-NOTIFY-008: Email retry tracking — EmailRetryCount and EmailStatus update correctly
    [Fact]
    public async Task Notification_EmailRetryTracking_UpdatesCountAndStatus()
    {
        using var context = CreateInMemoryContext(nameof(Notification_EmailRetryTracking_UpdatesCountAndStatus));

        var user = CreateTestUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.LeaveApproved,
            Title = "Leave Approved",
            Message = "Your request was approved.",
            EmailStatus = NotificationEmailStatus.Pending,
            EmailRetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Simulate 5 retries then final failure
        notification.EmailRetryCount = 5;
        notification.EmailStatus = NotificationEmailStatus.DeliveryFailed;
        await context.SaveChangesAsync();

        var failed = await context.Notifications.FirstAsync(n => n.Id == notification.Id);
        failed.EmailRetryCount.Should().Be(5);
        failed.EmailStatus.Should().Be(NotificationEmailStatus.DeliveryFailed);
    }
}
