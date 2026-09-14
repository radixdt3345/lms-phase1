using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F01-DB-001 — Authentication and Identity DB schema.
/// Covers UT-DB-001 through UT-DB-007.
/// </summary>
public class AuthenticationDbTests
{
    private static LmsDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    // UT-DB-001: User entity has all required properties with correct types
    [Fact]
    public void User_Entity_HasRequiredProperties()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            AzureAdObjectId = "aad-object-id-123",
            Email = "test.user@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.Id.Should().NotBeEmpty();
        user.AzureAdObjectId.Should().Be("aad-object-id-123");
        user.Email.Should().Be("test.user@example.com");
        user.DisplayName.Should().Be("Test User");
        user.Status.Should().Be(UserStatus.Active);
        user.IsActive.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
        user.DeletedAt.Should().BeNull();
        user.LockedAt.Should().BeNull();
        user.RefreshToken.Should().BeNull();
        user.UserRoles.Should().BeEmpty();
    }

    // UT-DB-002: Role entity has correct name values matching RoleNames constants
    [Fact]
    public void Role_Entity_HasCorrectNameConstants()
    {
        RoleNames.HRAdmin.Should().Be("HRAdmin");
        RoleNames.Manager.Should().Be("Manager");
        RoleNames.Employee.Should().Be("Employee");
        RoleNames.Director.Should().Be("Director");
        RoleNames.SuperAdmin.Should().Be("SuperAdmin");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = RoleNames.HRAdmin,
            Description = "HR Administrator",
            CreatedAt = DateTime.UtcNow
        };

        role.Name.Should().Be("HRAdmin");
        role.UserRoles.Should().BeEmpty();
    }

    // UT-DB-003: UserRole composite key and FK references work correctly
    [Fact]
    public void UserRole_CompositeKey_IsCorrectlyFormed()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            AzureAdObjectId = "aad-123",
            Email = "user@test.com",
            DisplayName = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var role = new Role
        {
            Id = roleId,
            Name = RoleNames.Employee,
            CreatedAt = DateTime.UtcNow
        };

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = "system",
            User = user,
            Role = role
        };

        userRole.UserId.Should().Be(userId);
        userRole.RoleId.Should().Be(roleId);
        userRole.User.Should().BeSameAs(user);
        userRole.Role.Should().BeSameAs(role);
        userRole.AssignedBy.Should().Be("system");
    }

    // UT-DB-004: LmsDbContext builds model without errors using InMemory provider
    [Fact]
    public void LmsDbContext_CanBuildModel_WithoutErrors()
    {
        using var context = CreateInMemoryContext(nameof(LmsDbContext_CanBuildModel_WithoutErrors));

        context.Should().NotBeNull();
        context.Users.Should().NotBeNull();
        context.Roles.Should().NotBeNull();
        context.UserRoles.Should().NotBeNull();
    }

    // UT-DB-005: DataSeeder creates system roles idempotently (safe to run multiple times)
    [Fact]
    public async Task DataSeeder_SeedsRoles_Idempotently()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsRoles_Idempotently));
        var logger = NullLogger<DataSeeder>.Instance;
        var seeder = new DataSeeder(context, logger);

        await seeder.SeedRolesAsync();
        await seeder.SeedRolesAsync();

        var roles = await context.Roles.ToListAsync();
        roles.Should().HaveCount(5);
        roles.Select(r => r.Name).Should().Contain(new[]
        {
            RoleNames.HRAdmin,
            RoleNames.Manager,
            RoleNames.Employee,
            RoleNames.Director,
            RoleNames.SuperAdmin
        });
    }

    // UT-DB-006: User soft-delete — DeletedAt set, record not hard-deleted
    [Fact]
    public async Task User_SoftDelete_SetsDeletedAt_NotHardDeleted()
    {
        using var context = CreateInMemoryContext(nameof(User_SoftDelete_SetsDeletedAt_NotHardDeleted));

        var user = new User
        {
            Id = Guid.NewGuid(),
            AzureAdObjectId = "soft-delete-test",
            Email = "softdelete@test.com",
            DisplayName = "Soft Delete User",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.Status = UserStatus.Inactive;
        await context.SaveChangesAsync();

        var allUsers = await context.Users.ToListAsync();
        allUsers.Should().HaveCount(1);
        allUsers[0].DeletedAt.Should().NotBeNull();
        allUsers[0].IsActive.Should().BeFalse();
    }

    // UT-DB-007: Account lockout — status changes to Locked after failed attempts
    [Fact]
    public async Task User_AccountLockout_SetsStatusToLocked()
    {
        using var context = CreateInMemoryContext(nameof(User_AccountLockout_SetsStatusToLocked));

        var user = new User
        {
            Id = Guid.NewGuid(),
            AzureAdObjectId = "lockout-test",
            Email = "lockout@test.com",
            DisplayName = "Lockout User",
            Status = UserStatus.Active,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.FailedLoginAttempts = 3;
        user.Status = UserStatus.Locked;
        user.LockedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var lockedUser = await context.Users.FirstAsync(u => u.Id == user.Id);
        lockedUser.Status.Should().Be(UserStatus.Locked);
        lockedUser.FailedLoginAttempts.Should().Be(3);
        lockedUser.LockedAt.Should().NotBeNull();
    }
}
