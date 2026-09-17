using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.ActorUserId)
            .HasColumnName("actor_user_id")
            .IsRequired();

        builder.Property(a => a.ActorEmail)
            .HasColumnName("actor_email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.RecordType)
            .HasColumnName("record_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.RecordId)
            .HasColumnName("record_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.OldValue)
            .HasColumnName("old_value")
            .HasColumnType("text");

        builder.Property(a => a.NewValue)
            .HasColumnName("new_value")
            .HasColumnType("text");

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        // Indexes — most queries filter/sort on timestamp + actor + record type
        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("idx_audit_logs_timestamp");

        builder.HasIndex(a => a.ActorUserId)
            .HasDatabaseName("idx_audit_logs_actor_user_id");

        builder.HasIndex(a => a.RecordType)
            .HasDatabaseName("idx_audit_logs_record_type");
    }
}
