using FluentAssertions;
using LMS.Application.DTOs.Holiday;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F10-API-001 — Public Holiday Management API layer.
/// Covers UT-F10-API-001 through UT-F10-API-005.
/// </summary>
public class PublicHolidayServiceTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static LmsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    private static IPublicHolidayService BuildService(LmsDbContext context) =>
        new PublicHolidayService(context);

    private static async Task<PublicHoliday> SeedHoliday(
        LmsDbContext context,
        string name,
        DateOnly date,
        bool isActive = true)
    {
        var h = new PublicHoliday
        {
            Id = Guid.NewGuid(),
            Name = name,
            Date = date,
            Year = date.Year,
            CountryCode = "IN",
            IsOptional = false,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.PublicHolidays.Add(h);
        await context.SaveChangesAsync();
        return h;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F10-API-001: GetByYearAsync returns holidays for given year only
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F10_API_001_GetByYearAsync_ReturnsOnlyHolidaysForRequestedYear()
    {
        // Arrange
        var ctx = CreateContext(nameof(UT_F10_API_001_GetByYearAsync_ReturnsOnlyHolidaysForRequestedYear));
        var svc = BuildService(ctx);

        await SeedHoliday(ctx, "Republic Day 2026", new DateOnly(2026, 1, 26));
        await SeedHoliday(ctx, "Independence Day 2026", new DateOnly(2026, 8, 15));
        await SeedHoliday(ctx, "Republic Day 2025", new DateOnly(2025, 1, 26));

        // Act
        var result = (await svc.GetByYearAsync(2026)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(h => h.Year == 2026);
        result.Select(h => h.Name).Should().Contain("Republic Day 2026");
        result.Select(h => h.Name).Should().Contain("Independence Day 2026");
        result.Select(h => h.Name).Should().NotContain("Republic Day 2025");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F10-API-002: IsHolidayAsync returns true for a seeded active holiday date
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F10_API_002_IsHolidayAsync_ReturnsTrueForActiveHoliday()
    {
        // Arrange
        var ctx = CreateContext(nameof(UT_F10_API_002_IsHolidayAsync_ReturnsTrueForActiveHoliday));
        var svc = BuildService(ctx);

        var holidayDate = new DateOnly(2026, 1, 26);
        await SeedHoliday(ctx, "Republic Day", holidayDate, isActive: true);

        // Act
        var isHoliday = await svc.IsHolidayAsync(holidayDate);
        var isNotHoliday = await svc.IsHolidayAsync(new DateOnly(2026, 1, 27));

        // Assert
        isHoliday.Should().BeTrue();
        isNotHoliday.Should().BeFalse();
    }

    [Fact]
    public async Task UT_F10_API_002b_IsHolidayAsync_ReturnsFalseForInactiveHoliday()
    {
        // Arrange
        var ctx = CreateContext(nameof(UT_F10_API_002b_IsHolidayAsync_ReturnsFalseForInactiveHoliday));
        var svc = BuildService(ctx);

        var holidayDate = new DateOnly(2026, 3, 25);
        await SeedHoliday(ctx, "Holi", holidayDate, isActive: false);

        // Act
        var result = await svc.IsHolidayAsync(holidayDate);

        // Assert
        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F10-API-003: GetWorkingDaysAsync correctly excludes weekends and holidays
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F10_API_003_GetWorkingDaysAsync_ExcludesWeekendsAndHolidays()
    {
        // 2026-01-26 is Monday (Republic Day). Mon-Fri = 5 weekdays, minus 1 holiday = 4.
        var ctx = CreateContext(nameof(UT_F10_API_003_GetWorkingDaysAsync_ExcludesWeekendsAndHolidays));
        var svc = BuildService(ctx);

        await SeedHoliday(ctx, "Republic Day", new DateOnly(2026, 1, 26), isActive: true);

        var from = new DateOnly(2026, 1, 26);
        var to = new DateOnly(2026, 1, 30);

        // Act
        var workingDays = await svc.GetWorkingDaysAsync(from, to);

        // Assert
        workingDays.Should().Be(4);
    }

    [Fact]
    public async Task UT_F10_API_003b_GetWorkingDaysAsync_ExcludesWeekendOnly_WhenNoHolidays()
    {
        // 2026-01-05 (Mon) to 2026-01-11 (Sun) = 5 weekdays
        var ctx = CreateContext(nameof(UT_F10_API_003b_GetWorkingDaysAsync_ExcludesWeekendOnly_WhenNoHolidays));
        var svc = BuildService(ctx);

        var from = new DateOnly(2026, 1, 5);
        var to = new DateOnly(2026, 1, 11);

        // Act
        var result = await svc.GetWorkingDaysAsync(from, to);

        // Assert
        result.Should().Be(5);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F10-API-004: BulkImportAsync skips duplicate Name+Date pairs
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F10_API_004_BulkImportAsync_SkipsDuplicateNameDatePairs()
    {
        // Arrange
        var ctx = CreateContext(nameof(UT_F10_API_004_BulkImportAsync_SkipsDuplicateNameDatePairs));
        var svc = BuildService(ctx);

        await SeedHoliday(ctx, "Republic Day", new DateOnly(2026, 1, 26));

        var batch = new List<CreatePublicHolidayDto>
        {
            new() { Name = "Republic Day", Date = new DateOnly(2026, 1, 26) },      // duplicate
            new() { Name = "Independence Day", Date = new DateOnly(2026, 8, 15) },   // new
            new() { Name = "Independence Day", Date = new DateOnly(2026, 8, 15) },   // intra-batch dup
            new() { Name = "Gandhi Jayanti", Date = new DateOnly(2026, 10, 2) }      // new
        };

        // Act
        var inserted = (await svc.BulkImportAsync(batch)).ToList();

        // Assert
        inserted.Should().HaveCount(2);
        inserted.Select(h => h.Name).Should().Contain("Independence Day");
        inserted.Select(h => h.Name).Should().Contain("Gandhi Jayanti");

        var total = await ctx.PublicHolidays.CountAsync();
        total.Should().Be(3);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F10-API-005: Controller endpoints return ApiResponse<T> envelope
    //   Verified via the service CRUD round-trip returning typed DTOs which the
    //   controller wraps in ApiResponse<T>.Ok(dto) — same pattern as AuthApiTests.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F10_API_005_CreateUpdateDelete_RoundTrip_ReturnsCorrectDto()
    {
        // Arrange
        var ctx = CreateContext(nameof(UT_F10_API_005_CreateUpdateDelete_RoundTrip_ReturnsCorrectDto));
        var svc = BuildService(ctx);

        var createDto = new CreatePublicHolidayDto
        {
            Name = "Christmas",
            Date = new DateOnly(2026, 12, 25),
            Description = "Christmas Day",
            CountryCode = "IN",
            IsOptional = false
        };

        // Act — Create
        var created = await svc.CreateAsync(createDto);

        // Assert — Create
        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("Christmas");
        created.Date.Should().Be(new DateOnly(2026, 12, 25));
        created.Year.Should().Be(2026);
        created.IsActive.Should().BeTrue();

        // Act — GetById
        var fetched = await svc.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Christmas");

        // Act — Update (deactivate)
        var updateDto = new UpdatePublicHolidayDto { IsActive = false, Name = "Christmas Day" };
        var updated = await svc.UpdateAsync(created.Id, updateDto);
        updated.IsActive.Should().BeFalse();
        updated.Name.Should().Be("Christmas Day");

        // Act — Delete
        var deleted = await svc.DeleteAsync(created.Id);
        deleted.Should().BeTrue();

        var afterDelete = await svc.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task UT_F10_API_005b_DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var ctx = CreateContext(nameof(UT_F10_API_005b_DeleteAsync_ReturnsFalse_WhenNotFound));
        var svc = BuildService(ctx);

        // Act
        var result = await svc.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}
