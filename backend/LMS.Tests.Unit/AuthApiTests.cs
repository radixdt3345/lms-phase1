using FluentAssertions;
using LMS.Application.DTOs.Auth;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F01-API-001 — Authentication &amp; Identity API layer.
/// Covers UT-API-001 through UT-API-007.
/// </summary>
public class AuthApiTests
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

    private static IUserService BuildService(LmsDbContext context) =>
        new UserService(context, NullLogger<UserService>.Instance);

    private static async Task<Role> SeedEmployeeRole(LmsDbContext context)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = RoleNames.Employee,
            Description = "Standard employee",
            CreatedAt = DateTime.UtcNow
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-001: GetOrCreateFromAzureAdAsync creates a new user on first call
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateFromAzureAdAsync_FirstCall_CreatesUser()
    {
        // Arrange
        using var context = CreateContext(nameof(GetOrCreateFromAzureAdAsync_FirstCall_CreatesUser));
        await SeedEmployeeRole(context);
        var svc = BuildService(context);

        // Act
        var dto = await svc.GetOrCreateFromAzureAdAsync("oid-001", "alice@example.com", "Alice");

        // Assert
        dto.Should().NotBeNull();
        dto!.AzureAdObjectId.Should().Be("oid-001");
        dto.Email.Should().Be("alice@example.com");
        dto.DisplayName.Should().Be("Alice");
        dto.IsActive.Should().BeTrue();
        dto.Roles.Should().Contain(RoleNames.Employee);

        var dbUser = await context.Users.SingleAsync();
        dbUser.AzureAdObjectId.Should().Be("oid-001");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-002: GetOrCreateFromAzureAdAsync returns the same user on second call
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateFromAzureAdAsync_SecondCall_ReturnsExistingUser()
    {
        // Arrange
        using var context = CreateContext(nameof(GetOrCreateFromAzureAdAsync_SecondCall_ReturnsExistingUser));
        await SeedEmployeeRole(context);
        var svc = BuildService(context);

        // Act
        var first = await svc.GetOrCreateFromAzureAdAsync("oid-002", "bob@example.com", "Bob");
        var second = await svc.GetOrCreateFromAzureAdAsync("oid-002", "bob@example.com", "Bob");

        // Assert
        first!.Id.Should().Be(second!.Id);
        (await context.Users.CountAsync()).Should().Be(1, "idempotent — no duplicate user created");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-003: GetByAzureAdObjectIdAsync returns null when user does not exist
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByAzureAdObjectIdAsync_UnknownOid_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByAzureAdObjectIdAsync_UnknownOid_ReturnsNull));
        var svc = BuildService(context);

        // Act
        var result = await svc.GetByAzureAdObjectIdAsync("no-such-oid");

        // Assert
        result.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-004: GetAllActiveUsersAsync returns only active users
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveUsersAsync_ReturnsOnlyActiveUsers()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllActiveUsersAsync_ReturnsOnlyActiveUsers));
        await SeedEmployeeRole(context);
        var svc = BuildService(context);

        await svc.GetOrCreateFromAzureAdAsync("oid-active", "active@example.com", "Active User");

        // Add an inactive user directly.
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            AzureAdObjectId = "oid-inactive",
            Email = "inactive@example.com",
            DisplayName = "Inactive User",
            IsActive = false,
            Status = UserStatus.Inactive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var users = (await svc.GetAllActiveUsersAsync()).ToList();

        // Assert
        users.Should().HaveCount(1);
        users[0].Email.Should().Be("active@example.com");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-005: AssignRoleAsync adds a role and returns ApiResponse-shaped DTO
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleAsync_AddsRole_AndReturnsDtoWithRole()
    {
        // Arrange
        using var context = CreateContext(nameof(AssignRoleAsync_AddsRole_AndReturnsDtoWithRole));
        var employeeRole = await SeedEmployeeRole(context);

        var hrRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = RoleNames.HRAdmin,
            CreatedAt = DateTime.UtcNow
        };
        context.Roles.Add(hrRole);
        await context.SaveChangesAsync();

        var svc = BuildService(context);
        var profile = await svc.GetOrCreateFromAzureAdAsync("oid-005", "carol@example.com", "Carol");

        // Act — assign an additional HRAdmin role
        var updated = await svc.AssignRoleAsync(profile!.Id, hrRole.Id, "superadmin");

        // Assert
        updated.Should().NotBeNull();
        updated.Roles.Should().Contain(RoleNames.HRAdmin);
        updated.Roles.Should().Contain(RoleNames.Employee);

        // Verify the shape matches what the controller wraps in ApiResponse<T>
        var envelope = LMS.Domain.Common.ApiResponse<UserProfileDto>.Ok(updated);
        envelope.Data.Should().BeSameAs(updated);
        envelope.Data.Roles.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-006: RemoveRoleAsync removes a role from the user
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveRoleAsync_RemovesRole_FromUser()
    {
        // Arrange
        using var context = CreateContext(nameof(RemoveRoleAsync_RemovesRole_FromUser));
        var employeeRole = await SeedEmployeeRole(context);
        var svc = BuildService(context);

        var profile = await svc.GetOrCreateFromAzureAdAsync("oid-006", "dave@example.com", "Dave");
        profile!.Roles.Should().Contain(RoleNames.Employee);

        // Act
        var updated = await svc.RemoveRoleAsync(profile.Id, employeeRole.Id);

        // Assert
        updated.Roles.Should().NotContain(RoleNames.Employee);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-API-007: GetAllRolesAsync returns all seeded roles
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRoles()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllRolesAsync_ReturnsAllRoles));

        var roleNames = new[] { RoleNames.Employee, RoleNames.Manager, RoleNames.HRAdmin, RoleNames.Director, RoleNames.SuperAdmin };
        foreach (var name in roleNames)
        {
            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var svc = BuildService(context);

        // Act
        var roles = (await svc.GetAllRolesAsync()).ToList();

        // Assert
        roles.Should().HaveCount(5);
        roles.Select(r => r.Name).Should().BeEquivalentTo(roleNames);

        // Verify DTO shape carries Id
        roles.All(r => r.Id != Guid.Empty).Should().BeTrue();
    }
}
