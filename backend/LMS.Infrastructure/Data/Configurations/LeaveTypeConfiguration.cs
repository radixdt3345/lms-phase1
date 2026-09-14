using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("leave_types");

        builder.HasKey(lt => lt.Id);

        builder.Property(lt => lt.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(lt => lt.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(lt => lt.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(lt => lt.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        builder.Property(lt => lt.AnnualDays)
            .HasColumnName("annual_days")
            .IsRequired();

        builder.Property(lt => lt.RequiresAttachment)
            .HasColumnName("requires_attachment")
            .HasDefaultValue(false);

        builder.Property(lt => lt.RequiresHrApproval)
            .HasColumnName("requires_hr_approval")
            .HasDefaultValue(false);

        builder.Property(lt => lt.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(lt => lt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(lt => lt.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(lt => lt.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(lt => lt.Code)
            .IsUnique()
            .HasDatabaseName("idx_leave_types_code");

        builder.HasIndex(lt => lt.DeletedAt)
            .HasDatabaseName("idx_leave_types_deleted_at");

        builder.HasIndex(lt => lt.IsActive)
            .HasDatabaseName("idx_leave_types_is_active");
    }
}
