using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F04-DB-001 — Leave Type and Policy DB schema.
/// Covers UT-DB-F04-001 through UT-DB-F04-006.
/// </summary>
public class LeavePolicyDbTests
{
    private static LmsDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    // UT-DB-F04-001: LeaveType entity has all required properties with correct defaults
    [Fact]
    public void LeaveType_Entity_HasRequiredProperties()
    {
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = "Casual Leave",
            Code = "CL",
            Description = "Casual / personal leave.",
            AnnualDays = 12,
            RequiresAttachment = false,
            RequiresHrApproval = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        leaveType.Id.Should().NotBeEmpty();
        leaveType.Name.Should().Be("Casual Leave");
        leaveType.Code.Should().Be("CL");
        leaveType.AnnualDays.Should().Be(12);
        leaveType.RequiresAttachment.Should().BeFalse();
        leaveType.RequiresHrApproval.Should().BeFalse();
        leaveType.IsActive.Should().BeTrue();
        leaveType.DeletedAt.Should().BeNull();
        leaveType.LeavePolicies.Should().BeEmpty();
    }

    // UT-DB-F04-002: LeavePolicy entity has correct defaults and FK reference
    [Fact]
    public void LeavePolicy_Entity_HasCorrectDefaults()
    {
        var leaveTypeId = Guid.NewGuid();
        var leaveType = new LeaveType
        {
            Id = leaveTypeId,
            Name = "Sick Leave",
            Code = "SL",
            AnnualDays = 6,
            RequiresAttachment = true,
            RequiresHrApproval = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var policy = new LeavePolicy
        {
            Id = Guid.NewGuid(),
            Name = "Default Policy — Sick Leave",
            LeaveTypeId = leaveTypeId,
            LeaveType = leaveType,
            ApplicableToRole = null,
            AnnualAllotment = 6,
            MaxCarryForward = 0,
            MaxConsecutiveDays = 30,
            MinNoticeDays = 0,
            AccrualMonthly = false,
            AccrualRate = null,
            IsActive = true,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        policy.Id.Should().NotBeEmpty();
        policy.LeaveTypeId.Should().Be(leaveTypeId);
        policy.LeaveType.Should().BeSameAs(leaveType);
        policy.ApplicableToRole.Should().BeNull();
        policy.MaxCarryForward.Should().Be(0);
        policy.MaxConsecutiveDays.Should().Be(30);
        policy.AccrualMonthly.Should().BeFalse();
        policy.AccrualRate.Should().BeNull();
        policy.IsActive.Should().BeTrue();
        policy.EffectiveTo.Should().BeNull();
    }

    // UT-DB-F04-003: LmsDbContext exposes LeaveType and LeavePolicy DbSets
    [Fact]
    public void LmsDbContext_ExposesLeaveType_And_LeavePolicy_DbSets()
    {
        using var context = CreateInMemoryContext(nameof(LmsDbContext_ExposesLeaveType_And_LeavePolicy_DbSets));
        context.Should().NotBeNull();
        context.LeaveTypes.Should().NotBeNull();
        context.LeavePolicies.Should().NotBeNull();
    }

    // UT-DB-F04-004: DataSeeder seeds exactly 5 leave types idempotently (AC-28)
    [Fact]
    public async Task DataSeeder_SeedsLeaveTypes_Idempotently_AC28()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsLeaveTypes_Idempotently_AC28));
        var seeder = new DataSeeder(context, NullLogger<DataSeeder>.Instance);

        await seeder.SeedLeaveTypesAsync();
        await seeder.SeedLeaveTypesAsync();

        var leaveTypes = await context.LeaveTypes.ToListAsync();
        leaveTypes.Should().HaveCount(5);

        leaveTypes.Select(lt => lt.Code).Should().Contain(new[]
        {
            LeaveTypeNames.CasualLeaveCode,
            LeaveTypeNames.SickLeaveCode,
            LeaveTypeNames.EarnedLeaveCode,
            LeaveTypeNames.CompOffCode,
            LeaveTypeNames.UnpaidLeaveCode
        });

        var casual = leaveTypes.Single(lt => lt.Code == LeaveTypeNames.CasualLeaveCode);
        casual.AnnualDays.Should().Be(12);
        casual.RequiresAttachment.Should().BeFalse();
        casual.RequiresHrApproval.Should().BeFalse();

        var sick = leaveTypes.Single(lt => lt.Code == LeaveTypeNames.SickLeaveCode);
        sick.AnnualDays.Should().Be(6);
        sick.RequiresAttachment.Should().BeTrue();
        sick.RequiresHrApproval.Should().BeTrue();

        var earned = leaveTypes.Single(lt => lt.Code == LeaveTypeNames.EarnedLeaveCode);
        earned.AnnualDays.Should().Be(1);

        var compOff = leaveTypes.Single(lt => lt.Code == LeaveTypeNames.CompOffCode);
        compOff.AnnualDays.Should().Be(0);

        var unpaid = leaveTypes.Single(lt => lt.Code == LeaveTypeNames.UnpaidLeaveCode);
        unpaid.AnnualDays.Should().Be(0);
    }

    // UT-DB-F04-005: LeavePolicy FK reference requires valid LeaveTypeId
    [Fact]
    public async Task LeavePolicy_FkReference_RequiresValidLeaveType()
    {
        using var context = CreateInMemoryContext(nameof(LeavePolicy_FkReference_RequiresValidLeaveType));

        var leaveTypeId = Guid.NewGuid();
        var leaveType = new LeaveType
        {
            Id = leaveTypeId,
            Name = "Annual Leave",
            Code = "AL",
            AnnualDays = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeaveTypes.Add(leaveType);
        await context.SaveChangesAsync();

        var policy = new LeavePolicy
        {
            Id = Guid.NewGuid(),
            Name = "Annual Leave Policy",
            LeaveTypeId = leaveTypeId,
            AnnualAllotment = 15,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeavePolicies.Add(policy);
        await context.SaveChangesAsync();

        var saved = await context.LeavePolicies
            .Include(p => p.LeaveType)
            .FirstAsync(p => p.Id == policy.Id);

        saved.LeaveTypeId.Should().Be(leaveTypeId);
        saved.LeaveType.Should().NotBeNull();
        saved.LeaveType.Code.Should().Be("AL");
    }

    // UT-DB-F04-006: LeaveType soft-delete
    [Fact]
    public async Task LeaveType_SoftDelete_SetsDeletedAt_NotHardDeleted()
    {
        using var context = CreateInMemoryContext(nameof(LeaveType_SoftDelete_SetsDeletedAt_NotHardDeleted));

        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = "Test Leave",
            Code = "TL",
            AnnualDays = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeaveTypes.Add(leaveType);
        await context.SaveChangesAsync();

        leaveType.DeletedAt = DateTime.UtcNow;
        leaveType.IsActive = false;
        await context.SaveChangesAsync();

        var allTypes = await context.LeaveTypes.ToListAsync();
        allTypes.Should().HaveCount(1);
        allTypes[0].DeletedAt.Should().NotBeNull();
        allTypes[0].IsActive.Should().BeFalse();
    }

    // UT-DB-F04-007: Seeder creates one default policy per leave type
    [Fact]
    public async Task DataSeeder_CreatesDefaultPolicy_PerLeaveType()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_CreatesDefaultPolicy_PerLeaveType));
        var seeder = new DataSeeder(context, NullLogger<DataSeeder>.Instance);

        await seeder.SeedLeaveTypesAsync();

        var policies = await context.LeavePolicies.ToListAsync();
        policies.Should().HaveCount(5);
        policies.Should().OnlyContain(p => p.MaxCarryForward == 0);
        policies.Should().OnlyContain(p => p.IsActive);
    }
}
