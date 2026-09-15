using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F13-DB-001 - Master Data and Seed Data DB Layer.
/// Covers PublicHoliday entity, DbSet registration, seeder idempotency,
/// holiday count, date uniqueness, and active-status defaults.
/// </summary>
public class MasterDataDbTests
{
    private static LmsDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    // UT-DB-F13-001: PublicHoliday entity has all required properties with correct defaults
    [Fact]
    public void PublicHoliday_Entity_HasRequiredProperties_WithCorrectDefaults()
    {
        var id = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 26);
        var now = DateTime.UtcNow;

        var holiday = new PublicHoliday
        {
            Id = id,
            Name = "Republic Day",
            Date = date,
            Year = 2026,
            Description = "India Republic Day",
            CountryCode = "IN",
            IsOptional = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        holiday.Id.Should().Be(id);
        holiday.Name.Should().Be("Republic Day");
        holiday.Date.Should().Be(date);
        holiday.Year.Should().Be(2026);
        holiday.Description.Should().Be("India Republic Day");
        holiday.CountryCode.Should().Be("IN");
        holiday.IsOptional.Should().BeFalse();
        holiday.IsActive.Should().BeTrue();
    }

    // UT-DB-F13-002: PublicHoliday defaults - IsActive true, IsOptional false, Description and CountryCode null
    [Fact]
    public void PublicHoliday_Entity_Defaults_AreCorrect()
    {
        var holiday = new PublicHoliday();

        holiday.IsActive.Should().BeTrue();
        holiday.IsOptional.Should().BeFalse();
        holiday.Name.Should().Be(string.Empty);
        holiday.Description.Should().BeNull();
        holiday.CountryCode.Should().BeNull();
    }

    // UT-DB-F13-003: LmsDbContext includes PublicHolidays DbSet and builds without errors
    [Fact]
    public void LmsDbContext_IncludesPublicHolidays_DbSet()
    {
        using var context = CreateInMemoryContext(nameof(LmsDbContext_IncludesPublicHolidays_DbSet));

        context.Should().NotBeNull();
        context.PublicHolidays.Should().NotBeNull();
    }

    // UT-DB-F13-004: DataSeeder seeds 2026 Indian holidays idempotently - running twice creates no duplicates
    [Fact]
    public async Task DataSeeder_SeedsPublicHolidays_Idempotently()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsPublicHolidays_Idempotently));
        var logger = NullLogger<DataSeeder>.Instance;
        var seeder = new DataSeeder(context, logger);

        // Run twice - second run must not duplicate records
        await seeder.SeedPublicHolidaysAsync();
        await seeder.SeedPublicHolidaysAsync();

        var holidays = await context.PublicHolidays.ToListAsync();
        holidays.Should().HaveCount(14, "14 distinct 2026 Indian national holidays are defined");
    }

    // UT-DB-F13-005: Seeded holidays all belong to year 2026, country IN, and are active
    [Fact]
    public async Task DataSeeder_SeedsPublicHolidays_WithCorrectYearCountryAndStatus()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsPublicHolidays_WithCorrectYearCountryAndStatus));
        var logger = NullLogger<DataSeeder>.Instance;
        var seeder = new DataSeeder(context, logger);

        await seeder.SeedPublicHolidaysAsync();

        var holidays = await context.PublicHolidays.ToListAsync();
        holidays.Should().AllSatisfy(h =>
        {
            h.Year.Should().Be(2026);
            h.CountryCode.Should().Be("IN");
            h.IsActive.Should().BeTrue();
        });
    }

    // UT-DB-F13-006: Seeded holiday dates are all unique - no two holidays share the same date
    [Fact]
    public async Task DataSeeder_SeedsPublicHolidays_DatesAreUnique()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsPublicHolidays_DatesAreUnique));
        var logger = NullLogger<DataSeeder>.Instance;
        var seeder = new DataSeeder(context, logger);

        await seeder.SeedPublicHolidaysAsync();

        var holidays = await context.PublicHolidays.ToListAsync();
        var dates = holidays.Select(h => h.Date).ToList();
        dates.Should().OnlyHaveUniqueItems("each holiday must fall on a distinct calendar date");
    }

    // UT-DB-F13-007: Seeded holidays all have non-empty names and valid years
    [Fact]
    public async Task DataSeeder_SeedsPublicHolidays_AllNamesNonEmpty()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsPublicHolidays_AllNamesNonEmpty));
        var logger = NullLogger<DataSeeder>.Instance;
        var seeder = new DataSeeder(context, logger);

        await seeder.SeedPublicHolidaysAsync();

        var holidays = await context.PublicHolidays.ToListAsync();
        holidays.Should().AllSatisfy(h =>
        {
            h.Name.Should().NotBeNullOrWhiteSpace();
            h.Year.Should().BeGreaterThan(2000);
        });
    }

    // UT-DB-F13-008: PublicHoliday entity type exists in EF model and carries expected index configuration
    [Fact]
    public void PublicHoliday_EntityModel_HasExpectedIndexConfiguration()
    {
        using var context = CreateInMemoryContext(nameof(PublicHoliday_EntityModel_HasExpectedIndexConfiguration));

        var entityType = context.Model.FindEntityType(typeof(PublicHoliday));
        entityType.Should().NotBeNull("PublicHoliday must be registered in the EF model");

        var indexes = entityType!.GetIndexes().ToList();

        // Year + Date composite index
        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "Year") &&
            i.Properties.Any(p => p.Name == "Date"),
            "composite index on (Year, Date) must exist");

        // CountryCode index
        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "CountryCode"),
            "index on CountryCode must exist");
    }
}
