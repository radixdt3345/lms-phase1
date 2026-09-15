using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(d => d.OverlapLimit)
            .HasColumnName("overlap_limit")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(d => d.DeletedAt)
            .HasColumnName("deleted_at");

        // Unique index on name (among active departments — constraint enforced at application layer
        // by checking IsActive; DB index covers uniqueness for all records to support case-insensitive checks)
        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasDatabaseName("idx_departments_name");

        // Unique index on code
        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasDatabaseName("idx_departments_code");

        // Index for soft-delete filtering
        builder.HasIndex(d => d.DeletedAt)
            .HasDatabaseName("idx_departments_deleted_at");

        // One Department has many Users (nullable FK on User side)
        builder.HasMany(d => d.Users)
            .WithOne(u => u.DepartmentEntity)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
