using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.AzureAdObjectId)
            .HasColumnName("azure_ad_object_id")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512);

        builder.Property(u => u.EmployeeCode)
            .HasColumnName("employee_code")
            .HasMaxLength(64);

        builder.Property(u => u.Department)
            .HasColumnName("department")
            .HasMaxLength(128);

        builder.Property(u => u.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(u => u.ManagerAzureAdObjectId)
            .HasColumnName("manager_azure_ad_object_id")
            .HasMaxLength(128);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0);

        builder.Property(u => u.LockedAt)
            .HasColumnName("locked_at");

        builder.Property(u => u.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(512);

        builder.Property(u => u.RefreshTokenExpiresAt)
            .HasColumnName("refresh_token_expires_at");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(u => u.AzureAdObjectId)
            .IsUnique()
            .HasDatabaseName("idx_users_azure_ad_object_id");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email");

        builder.HasIndex(u => u.DeletedAt)
            .HasDatabaseName("idx_users_deleted_at");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("idx_users_status");

        builder.HasIndex(u => u.DepartmentId)
            .HasDatabaseName("idx_users_department_id");
    }
}
