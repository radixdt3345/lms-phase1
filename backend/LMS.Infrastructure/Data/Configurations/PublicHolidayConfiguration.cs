using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("public_holidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(h => h.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(h => h.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(h => h.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(h => h.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2);

        builder.Property(h => h.IsOptional)
            .HasColumnName("is_optional")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(h => h.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Composite index for year+date queries
        builder.HasIndex(h => new { h.Year, h.Date })
            .HasDatabaseName("idx_public_holidays_year_date");

        // Index for active holiday lookups by year
        builder.HasIndex(h => new { h.Year, h.IsActive })
            .HasDatabaseName("idx_public_holidays_year_is_active");

        // Index for country-code filtering
        builder.HasIndex(h => h.CountryCode)
            .HasDatabaseName("idx_public_holidays_country_code");
    }
}
