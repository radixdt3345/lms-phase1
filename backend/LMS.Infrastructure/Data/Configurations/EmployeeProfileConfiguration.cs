using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("employee_profiles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.EmployeeCode)
            .HasColumnName("employee_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.JobTitle)
            .HasColumnName("job_title")
            .HasMaxLength(100);

        builder.Property(e => e.JoiningDate)
            .HasColumnName("joining_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(e => e.TerminationDate)
            .HasColumnName("termination_date")
            .HasColumnType("date");

        builder.Property(e => e.EmploymentType)
            .HasColumnName("employment_type")
            .HasMaxLength(50)
            .HasDefaultValue("Full-Time")
            .IsRequired();

        builder.Property(e => e.ManagerUserId)
            .HasColumnName("manager_user_id");

        builder.Property(e => e.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("idx_employee_profiles_user_id");

        builder.HasIndex(e => e.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("idx_employee_profiles_employee_code");

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<EmployeeProfile>(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_profiles_user_id");

        builder.HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false)
            .HasConstraintName("fk_employee_profiles_manager_user_id");

        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false)
            .HasConstraintName("fk_employee_profiles_department_id");
    }
}
