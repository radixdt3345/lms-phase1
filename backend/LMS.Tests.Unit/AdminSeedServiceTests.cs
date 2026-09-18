using FluentAssertions;
using LMS.Application.DTOs.MasterData;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F-14 — Initial Data Seeding: API layer.
/// Covers ISeedService / SeedService behaviour (UT-F14-API-001 through UT-F14-API-005).
///
/// AC-67: Running the seed twice never creates duplicates.
/// AC-68: A fresh DB seed creates the expected counts.
/// FR-92: Seed is idempotent.
/// </summary>
public class AdminSeedServiceTests
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

    private static ISeedService BuildSeedService(LmsDbContext context, DataSeeder? seeder = null)
    {
        seeder ??= CreateSeeder(context);
        return new SeedService(context, seeder, NullLogger<SeedService>.Instance);
    }

    private static async Task AddMinimalSeedData(LmsDbContext context)
    {
        context.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            Name = "SuperAdmin",
            Description = "Super Administrator",
            CreatedAt = DateTime.UtcNow
        });
        context.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            Name = "Human Resources",
            Code = "HR",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.LeaveTypes.Add(new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = "Casual Leave",
            Code = "CL",
            AnnualDays = 12,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F14-API-001: GetSeedStatusAsync — healthy when all entity types present
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSeedStatusAsync_AllEntitiesPresent_ReturnsIsHealthyTrue()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f14-001");
        await AddMinimalSeedData(ctx);
        var svc = BuildSeedService(ctx);

        // Act
        var result = await svc.GetSeedStatusAsync();

        // Assert
        result.Should().BeOfType<SeedStatusDto>();
        result.IsHealthy.Should().BeTrue("all required entity types are present");
        result.TotalRoles.Should().BeGreaterThan(0);
        result.TotalDepartments.Should().BeGreaterThan(0);
        result.TotalLeaveTypes.Should().BeGreaterThan(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F14-API-002: GetSeedStatusAsync — not healthy on empty database
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSeedStatusAsync_EmptyDatabase_ReturnsIsHealthyFalse()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f14-002");
        var svc = BuildSeedService(ctx);

        // Act
        var result = await svc.GetSeedStatusAsync();

        // Assert
        result.IsHealthy.Should().BeFalse("no entities exist yet");
        result.TotalRoles.Should().Be(0);
        result.TotalDepartments.Should().Be(0);
        result.TotalLeaveTypes.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F14-API-003: TriggerReseedAsync "all" — seeds every section, returns true
    // Also verifies AC-68: at least the expected entity types exist after seeding
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerReseedAsync_AllSection_SeedsEverySection_AndReturnsTrue()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f14-003");
        var svc = BuildSeedService(ctx);

        // Act
        var result = await svc.TriggerReseedAsync("all");

        // Assert
        result.Should().BeTrue();
        var status = await svc.GetSeedStatusAsync();
        status.TotalRoles.Should().BeGreaterThan(0, "roles seeded by 'all'");
        status.TotalDepartments.Should().BeGreaterThan(0, "departments seeded by 'all'");
        status.TotalLeaveTypes.Should().BeGreaterThan(0, "leave types seeded by 'all'");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F14-API-004: TriggerReseedAsync is idempotent (AC-67, FR-92)
    // Running twice must not create duplicate records
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerReseedAsync_RunTwice_DoesNotCreateDuplicates()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f14-004");
        var svc = BuildSeedService(ctx);

        // Act — run seed twice
        await svc.TriggerReseedAsync("all");
        var countAfterFirst = await ctx.Roles.CountAsync();

        await svc.TriggerReseedAsync("all");
        var countAfterSecond = await ctx.Roles.CountAsync();

        // Assert — count must not change after the second run (idempotent)
        countAfterSecond.Should().Be(countAfterFirst,
            "AC-67: running the seed script twice must not create duplicate records");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F14-API-005: TriggerReseedAsync — unknown section returns false
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerReseedAsync_UnknownSection_ReturnsFalse()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f14-005");
        var svc = BuildSeedService(ctx);

        // Act
        var result = await svc.TriggerReseedAsync("unknown-garbage-section");

        // Assert
        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F14-API-006: TriggerReseedAsync — "roles" section seeds only roles
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerReseedAsync_RolesSection_SeedsRoles_AndReturnsTrue()
    {
        // Arrange
        await using var ctx = CreateContext("ut-f14-006");
        var svc = BuildSeedService(ctx);

        // Act
        var result = await svc.TriggerReseedAsync("roles");

        // Assert
        result.Should().BeTrue();
        var status = await svc.GetSeedStatusAsync();
        status.TotalRoles.Should().BeGreaterThan(0);
    }
}
