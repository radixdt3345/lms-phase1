using FluentAssertions;
using LMS.Application.DTOs.MasterData;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F13-API-001 — Master Data &amp; Seed Data API layer.
/// Covers UT-F13-API-001 through UT-F13-API-004.
/// </summary>
public class MasterDataServiceTests
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

    private static DataSeeder CreateSeeder(LmsDbContext context) =>
        new DataSeeder(context, NullLogger<DataSeeder>.Instance);

    private static IMasterDataService BuildService(LmsDbContext context, DataSeeder? seeder = null)
    {
        seeder ??= CreateSeeder(context);
        return new MasterDataService(context, seeder, NullLogger<MasterDataService>.Instance);
    }

    private static async Task SeedMasterData(LmsDbContext context)
    {
        context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "HRAdmin", Description = "HR", CreatedAt = DateTime.UtcNow });
        context.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Engineering", Code = "ENG", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.LeaveTypes.Add(new LeaveType { Id = Guid.NewGuid(), Name = "Casual", Code = "CL", AnnualDays = 12, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.PublicHolidays.Add(new PublicHoliday { Id = Guid.NewGuid(), Name = "Republic Day", Date = new DateOnly(2026, 1, 26), Year = 2026, CountryCode = "IN", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F13-API-001: GetSeedStatusAsync returns IsHealthy=true when all counts > 0
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSeedStatusAsync_AllEntitiesPresent_ReturnsIsHealthyTrue()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-001");
        await SeedMasterData(ctx);
        var svc = BuildService(ctx);

        // Act
        var result = await svc.GetSeedStatusAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.TotalRoles.Should().BeGreaterThan(0);
        result.TotalDepartments.Should().BeGreaterThan(0);
        result.TotalLeaveTypes.Should().BeGreaterThan(0);
        result.TotalPublicHolidays.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSeedStatusAsync_EmptyDb_ReturnsIsHealthyFalse()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-001b");
        var svc = BuildService(ctx);

        // Act
        var result = await svc.GetSeedStatusAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.TotalRoles.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F13-API-002: TriggerReseedAsync with "all" calls all seeder methods
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerReseedAsync_WithAll_ReturnsTrue()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-002");
        var svc = BuildService(ctx);

        // Act
        var result = await svc.TriggerReseedAsync("all");

        // Assert
        result.Should().BeTrue();
        // Verify all sections were seeded
        var status = await svc.GetSeedStatusAsync();
        status.TotalRoles.Should().BeGreaterThan(0, "roles should be seeded by 'all'");
        status.TotalDepartments.Should().BeGreaterThan(0, "departments should be seeded by 'all'");
        status.TotalLeaveTypes.Should().BeGreaterThan(0, "leave types should be seeded by 'all'");
        status.TotalPublicHolidays.Should().BeGreaterThan(0, "public holidays should be seeded by 'all'");
    }

    [Fact]
    public async Task TriggerReseedAsync_WithInvalidSection_ReturnsFalse()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-002b");
        var svc = BuildService(ctx);

        // Act
        var result = await svc.TriggerReseedAsync("invalid-section");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerReseedAsync_WithRoles_OnlyCallsRoleSeed()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-002c");
        var svc = BuildService(ctx);

        // Act
        var result = await svc.TriggerReseedAsync("roles");

        // Assert
        result.Should().BeTrue();
        var status = await svc.GetSeedStatusAsync();
        status.TotalRoles.Should().BeGreaterThan(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F13-API-003: UpdateSystemConfigAsync rejects non-editable config
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSystemConfigAsync_NonEditableConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-003");
        ctx.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            Key = "ReadOnlyKey",
            Value = "original",
            Description = "A locked config",
            IsEditable = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var dto = new UpdateSystemConfigDto { Value = "new-value" };

        // Act & Assert
        await svc.Invoking(s => s.UpdateSystemConfigAsync("ReadOnlyKey", dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not editable*");
    }

    [Fact]
    public async Task UpdateSystemConfigAsync_EditableConfig_ReturnsUpdatedDto()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-003b");
        ctx.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            Key = "WorkWeekDays",
            Value = "Monday,Tuesday,Wednesday,Thursday,Friday",
            IsEditable = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var dto = new UpdateSystemConfigDto { Value = "Monday,Tuesday,Wednesday,Thursday" };

        // Act
        var result = await svc.UpdateSystemConfigAsync("WorkWeekDays", dto);

        // Assert
        result.Key.Should().Be("WorkWeekDays");
        result.Value.Should().Be("Monday,Tuesday,Wednesday,Thursday");
    }

    [Fact]
    public async Task UpdateSystemConfigAsync_UnknownKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-003c");
        var svc = BuildService(ctx);
        var dto = new UpdateSystemConfigDto { Value = "anything" };

        // Act & Assert
        await svc.Invoking(s => s.UpdateSystemConfigAsync("NoSuchKey", dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F13-API-004: Controller endpoints return ApiResponse<T> envelope
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSeedStatusAsync_WrapsResponseInApiResponse()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-004");
        await SeedMasterData(ctx);
        var svc = BuildService(ctx);

        // Act
        var result = await svc.GetSeedStatusAsync();

        // Assert — verify the service returns a typed DTO that maps to ApiResponse<SeedStatusDto>
        result.Should().BeOfType<SeedStatusDto>();
        result.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task GetSystemConfigsAsync_ReturnsAllConfigs()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-004b");
        ctx.SystemConfigs.AddRange(
            new SystemConfig { Id = Guid.NewGuid(), Key = "Key1", Value = "Val1", IsEditable = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new SystemConfig { Id = Guid.NewGuid(), Key = "Key2", Value = "Val2", IsEditable = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);

        // Act
        var result = (await svc.GetSystemConfigsAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(c => c.Key == "Key1" && c.IsEditable);
        result.Should().ContainSingle(c => c.Key == "Key2" && !c.IsEditable);
    }

    [Fact]
    public async Task GetSystemConfigAsync_ExistingKey_ReturnsDto()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-004c");
        ctx.SystemConfigs.Add(new SystemConfig { Id = Guid.NewGuid(), Key = "DefaultCountryCode", Value = "IN", IsEditable = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);

        // Act
        var result = await svc.GetSystemConfigAsync("DefaultCountryCode");

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be("DefaultCountryCode");
        result.Value.Should().Be("IN");
    }

    [Fact]
    public async Task GetSystemConfigAsync_UnknownKey_ReturnsNull()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f13-004d");
        var svc = BuildService(ctx);

        // Act
        var result = await svc.GetSystemConfigAsync("DoesNotExist");

        // Assert
        result.Should().BeNull();
    }
}
