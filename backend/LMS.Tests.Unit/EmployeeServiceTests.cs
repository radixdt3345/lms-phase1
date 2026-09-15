using FluentAssertions;
using LMS.Application.DTOs.Employee;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F02-API-001 — Employee Management API layer.
/// Covers UT-F02-API-001 through UT-F02-API-006.
/// </summary>
public class EmployeeServiceTests
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

    private static IEmployeeService BuildService(LmsDbContext context) =>
        new EmployeeService(context, NullLogger<EmployeeService>.Instance);

    private static User CreateUser(LmsDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            AzureAdObjectId = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        return user;
    }

    private static async Task<EmployeeProfile> SeedProfile(
        LmsDbContext context,
        Guid userId,
        string code = "EMP001",
        DateTimeOffset? deletedAt = null)
    {
        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployeeCode = code,
            JobTitle = "Engineer",
            JoiningDate = DateOnly.FromDateTime(DateTime.Today),
            EmploymentType = "Full-Time",
            DeletedAt = deletedAt
        };
        context.EmployeeProfiles.Add(profile);
        await context.SaveChangesAsync();
        return profile;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F02-API-001: GetAllAsync returns only non-deleted profiles
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedProfiles()
    {
        using var context = CreateContext(nameof(GetAllAsync_ExcludesSoftDeletedProfiles));
        var user1 = CreateUser(context);
        var user2 = CreateUser(context);
        await context.SaveChangesAsync();

        await SeedProfile(context, user1.Id, "EMP001");
        await SeedProfile(context, user2.Id, "EMP002", deletedAt: DateTimeOffset.UtcNow);

        var service = BuildService(context);
        var result = await service.GetAllAsync(1, 20);

        result.Items.Should().HaveCount(1);
        result.Items.Single().EmployeeCode.Should().Be("EMP001");
        result.TotalCount.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F02-API-002: GetAllAsync paging works
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Paging_ReturnsCorrectPage()
    {
        using var context = CreateContext(nameof(GetAllAsync_Paging_ReturnsCorrectPage));
        for (int i = 1; i <= 5; i++)
        {
            var user = CreateUser(context);
            await context.SaveChangesAsync();
            await SeedProfile(context, user.Id, $"EMP{i:D3}");
        }

        var service = BuildService(context);
        var result = await service.GetAllAsync(page: 2, pageSize: 2);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F02-API-003: GetByIdAsync returns null for missing or soft-deleted
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForMissingOrDeleted()
    {
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsNull_ForMissingOrDeleted));
        var user = CreateUser(context);
        await context.SaveChangesAsync();
        var profile = await SeedProfile(context, user.Id, "EMP001", deletedAt: DateTimeOffset.UtcNow);

        var service = BuildService(context);

        var missingResult = await service.GetByIdAsync(Guid.NewGuid());
        var deletedResult = await service.GetByIdAsync(profile.Id);

        missingResult.Should().BeNull();
        deletedResult.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F02-API-004: CreateAsync succeeds and rejects duplicate codes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesProfile_AndRejectsDuplicateCode()
    {
        using var context = CreateContext(nameof(CreateAsync_CreatesProfile_AndRejectsDuplicateCode));
        var user = CreateUser(context);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        var dto = new CreateEmployeeProfileDto
        {
            UserId = user.Id,
            EmployeeCode = "EMP001",
            JoiningDate = DateOnly.FromDateTime(DateTime.Today),
            EmploymentType = "Full-Time"
        };

        var created = await service.CreateAsync(dto);
        created.EmployeeCode.Should().Be("EMP001");

        // Duplicate code should throw
        var act = async () => await service.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EMP001*");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F02-API-005: UpdateAsync applies changes and throws for missing
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AppliesChanges_AndThrowsForMissing()
    {
        using var context = CreateContext(nameof(UpdateAsync_AppliesChanges_AndThrowsForMissing));
        var user = CreateUser(context);
        await context.SaveChangesAsync();
        var profile = await SeedProfile(context, user.Id, "EMP001");

        var service = BuildService(context);

        var updated = await service.UpdateAsync(profile.Id, new UpdateEmployeeProfileDto
        {
            JobTitle = "Senior Engineer"
        });

        updated.JobTitle.Should().Be("Senior Engineer");

        var act = async () => await service.UpdateAsync(Guid.NewGuid(), new UpdateEmployeeProfileDto());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F02-API-006: DeleteAsync soft-deletes and returns false for missing
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletes_AndReturnsFalseForMissing()
    {
        using var context = CreateContext(nameof(DeleteAsync_SoftDeletes_AndReturnsFalseForMissing));
        var user = CreateUser(context);
        await context.SaveChangesAsync();
        var profile = await SeedProfile(context, user.Id, "EMP001");

        var service = BuildService(context);

        var result = await service.DeleteAsync(profile.Id);
        result.Should().BeTrue();

        var afterDelete = await service.GetByIdAsync(profile.Id);
        afterDelete.Should().BeNull();

        var secondDelete = await service.DeleteAsync(profile.Id);
        secondDelete.Should().BeFalse();

        var missingDelete = await service.DeleteAsync(Guid.NewGuid());
        missingDelete.Should().BeFalse();
    }
}
