using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class LeavePolicyConfiguration : IEntityTypeConfiguration<LeavePolicy>
{
    public void Configure(EntityTypeBuilder<LeavePolicy> builder)
    {
        builder.ToTable("leave_policies");

        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(lp => lp.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(lp => lp.LeaveTypeId)
            .HasColumnName("leave_type_id")
            .IsRequired();

        builder.Property(lp => lp.ApplicableToRole)
            .HasColumnName("applicable_to_role")
            .HasMaxLength(64);

        builder.Property(lp => lp.AnnualAllotment)
            .HasColumnName("annual_allotment")
            .IsRequired();

        builder.Property(lp => lp.MaxCarryForward)
            .HasColumnName("max_carry_forward")
            .HasDefaultValue(0);

        builder.Property(lp => lp.MaxConsecutiveDays)
            .HasColumnName("max_consecutive_days")
            .HasDefaultValue(30);

        builder.Property(lp => lp.MinNoticeDays)
            .HasColumnName("min_notice_days")
            .HasDefaultValue(0);

        builder.Property(lp => lp.AccrualMonthly)
            .HasColumnName("accrual_monthly")
            .HasDefaultValue(false);

        builder.Property(lp => lp.AccrualRate)
            .HasColumnName("accrual_rate")
            .HasPrecision(5, 2);

        builder.Property(lp => lp.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(lp => lp.EffectiveFrom)
            .HasColumnName("effective_from")
            .IsRequired();

        builder.Property(lp => lp.EffectiveTo)
            .HasColumnName("effective_to");

        builder.Property(lp => lp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(lp => lp.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(lp => lp.LeaveType)
            .WithMany(lt => lt.LeavePolicies)
            .HasForeignKey(lp => lp.LeaveTypeId)
            .HasConstraintName("fk_leave_policies_leave_type_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(lp => lp.LeaveTypeId)
            .HasDatabaseName("idx_leave_policies_leave_type_id");

        builder.HasIndex(lp => lp.IsActive)
            .HasDatabaseName("idx_leave_policies_is_active");

        builder.HasIndex(lp => lp.EffectiveFrom)
            .HasDatabaseName("idx_leave_policies_effective_from");
    }
}
