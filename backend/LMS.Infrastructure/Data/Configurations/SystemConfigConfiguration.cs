using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Value)
            .HasColumnName("value")
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(c => c.IsEditable)
            .HasColumnName("is_editable")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(c => c.Key)
            .IsUnique()
            .HasDatabaseName("idx_system_configs_key_unique");
    }
}
