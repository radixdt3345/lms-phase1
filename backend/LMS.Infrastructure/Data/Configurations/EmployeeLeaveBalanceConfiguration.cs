using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class EmployeeLeaveBalanceConfiguration : IEntityTypeConfiguration<EmployeeLeaveBalance>
{
    public void Configure(EntityTypeBuilder<EmployeeLeaveBalance> builder)
    {
        builder.ToTable("employee_leave_balances");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.LeaveTypeId)
            .HasColumnName("leave_type_id")
            .IsRequired();

        builder.Property(e => e.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(e => e.TotalAllocated)
            .HasColumnName("total_allocated")
            .HasPrecision(5, 1)
            .IsRequired();

        builder.Property(e => e.Used)
            .HasColumnName("used")
            .HasPrecision(5, 1)
            .IsRequired();

        builder.Property(e => e.Carried)
            .HasColumnName("carried")
            .HasPrecision(5, 1)
            .IsRequired();

        builder.Property(e => e.Available)
            .HasColumnName("available")
            .HasPrecision(5, 1)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.LeaveTypeId, e.Year })
            .IsUnique()
            .HasDatabaseName("idx_employee_leave_balances_user_leavetype_year");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_leave_balances_user_id");

        builder.HasOne(e => e.LeaveType)
            .WithMany()
            .HasForeignKey(e => e.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_leave_balances_leave_type_id");
    }
}
