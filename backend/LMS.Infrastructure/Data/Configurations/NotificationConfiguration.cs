using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(n => n.RecipientUserId)
            .HasColumnName("recipient_user_id")
            .IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        builder.Property(n => n.RelatedEntityId)
            .HasColumnName("related_record_id");

        builder.Property(n => n.RelatedEntityType)
            .HasColumnName("related_record_type")
            .HasMaxLength(100);

        builder.Property(n => n.EmailStatus)
            .HasColumnName("email_status")
            .HasMaxLength(50)
            .HasDefaultValue(NotificationEmailStatus.NotSent)
            .IsRequired();

        builder.Property(n => n.EmailRetryCount)
            .HasColumnName("email_retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Foreign key: notifications.recipient_user_id → users.id (Restrict)
        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notifications_recipient_user_id");

        // Index for all notifications by user
        builder.HasIndex(n => n.RecipientUserId)
            .HasDatabaseName("idx_notifications_recipient_user_id");

        // Composite index for unread count queries
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead })
            .HasDatabaseName("idx_notifications_recipient_user_id_is_read");

        // Index for latest-first ordering
        builder.HasIndex(n => n.CreatedAt)
            .IsDescending(true)
            .HasDatabaseName("idx_notifications_created_at_desc");
    }
}
