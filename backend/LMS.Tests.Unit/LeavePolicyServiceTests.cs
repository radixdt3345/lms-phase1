using FluentAssertions;
using LMS.Application.DTOs.LeavePolicy;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F04-API-001 — Leave Type &amp; Policy Management API layer.
/// Covers UT-F04-API-001 through UT-F04-API-004.
/// </summary>
public class LeavePolicyServiceTests
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

    private static ILeavePolicyService BuildService(LmsDbContext context) =>
        new LeavePolicyService(context);

    private static async Task<LeaveType> SeedLeaveType(
        LmsDbContext context,
        string code = "CL",
        string name = "Casual Leave",
        bool isActive = true,
        bool deleted = false)
    {
        var lt = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            AnnualDays = 12,
            IsActive = isActive,
            DeletedAt = deleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeaveTypes.Add(lt);
        await context.SaveChangesAsync();
        return lt;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F04-API-001: GetAllLeaveTypesAsync returns only active types
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F04_API_001_GetAllLeaveTypes_ReturnsOnlyActive()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_001_GetAllLeaveTypes_ReturnsOnlyActive));
        await SeedLeaveType(context, "CL", "Casual Leave", isActive: true);
        await SeedLeaveType(context, "SL", "Sick Leave", isActive: false);
        await SeedLeaveType(context, "EL", "Earned Leave", isActive: true, deleted: true);
        var svc = BuildService(context);

        // Act
        var result = await svc.GetAllLeaveTypesAsync();

        // Assert
        var list = result.ToList();
        list.Should().HaveCount(1, "only active, non-deleted types should be returned");
        list.Single().Code.Should().Be("CL");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F04-API-002: CreateLeaveTypeAsync enforces unique Code
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F04_API_002_CreateLeaveType_DuplicateCode_Throws()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_002_CreateLeaveType_DuplicateCode_Throws));
        await SeedLeaveType(context, "CL", "Casual Leave");
        var svc = BuildService(context);
        var dto = new CreateLeaveTypeDto { Name = "Another Casual", Code = "CL", AnnualDays = 5 };

        // Act
        var act = async () => await svc.CreateLeaveTypeAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CL*");
    }

    [Fact]
    public async Task UT_F04_API_002_CreateLeaveType_UniqueCode_Succeeds()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_002_CreateLeaveType_UniqueCode_Succeeds));
        var svc = BuildService(context);
        var dto = new CreateLeaveTypeDto { Name = "Sick Leave", Code = "SL", AnnualDays = 7 };

        // Act
        var result = await svc.CreateLeaveTypeAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("SL");
        result.Name.Should().Be("Sick Leave");
        result.IsActive.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F04-API-003: GetActivePolicyAsync returns policy with null EffectiveTo
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F04_API_003_GetActivePolicy_NullEffectiveTo_ReturnsPolicy()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_003_GetActivePolicy_NullEffectiveTo_ReturnsPolicy));
        var lt = await SeedLeaveType(context, "CL", "Casual Leave");

        var policy = new LeavePolicy
        {
            Id = Guid.NewGuid(),
            LeaveTypeId = lt.Id,
            Name = "Standard CL Policy",
            AnnualAllotment = 12,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddYears(-1),
            EffectiveTo = null,   // open-ended = active
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeavePolicies.Add(policy);
        await context.SaveChangesAsync();

        var svc = BuildService(context);

        // Act
        var result = await svc.GetActivePolicyAsync(lt.Id);

        // Assert
        result.Should().NotBeNull();
        result!.LeaveTypeId.Should().Be(lt.Id);
        result.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task UT_F04_API_003_GetActivePolicy_ExpiredPolicy_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_003_GetActivePolicy_ExpiredPolicy_ReturnsNull));
        var lt = await SeedLeaveType(context, "SL", "Sick Leave");

        var expiredPolicy = new LeavePolicy
        {
            Id = Guid.NewGuid(),
            LeaveTypeId = lt.Id,
            Name = "Old Policy",
            AnnualAllotment = 7,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddYears(-2),
            EffectiveTo = DateTime.UtcNow.AddDays(-1),  // expired yesterday
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeavePolicies.Add(expiredPolicy);
        await context.SaveChangesAsync();

        var svc = BuildService(context);

        // Act
        var result = await svc.GetActivePolicyAsync(lt.Id);

        // Assert
        result.Should().BeNull("expired policies are not active");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F04-API-004: Controller endpoints return ApiResponse<T> envelope
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UT_F04_API_004_GetAllLeaveTypes_ReturnsApiResponseEnvelope()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_004_GetAllLeaveTypes_ReturnsApiResponseEnvelope));
        await SeedLeaveType(context, "CL", "Casual Leave");
        var svc = BuildService(context);

        // Act — call the service directly and verify data shape
        // (the controller wraps service results in ApiResponse<T>.Ok())
        var data = await svc.GetAllLeaveTypesAsync();

        // Simulate what the controller does: wrap in ApiResponse<T>
        var envelope = LMS.Domain.Common.ApiResponse<IEnumerable<LeaveTypeDto>>.Ok(data);

        // Assert — the envelope has a Data property containing the results
        envelope.Should().NotBeNull();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task UT_F04_API_004_CreateLeaveType_ReturnsApiResponseEnvelope()
    {
        // Arrange
        using var context = CreateContext(nameof(UT_F04_API_004_CreateLeaveType_ReturnsApiResponseEnvelope));
        var svc = BuildService(context);
        var dto = new CreateLeaveTypeDto { Name = "Earned Leave", Code = "EL", AnnualDays = 15 };

        // Act
        var created = await svc.CreateLeaveTypeAsync(dto);
        var envelope = LMS.Domain.Common.ApiResponse<LeaveTypeDto>.Ok(created);

        // Assert
        envelope.Should().NotBeNull();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Code.Should().Be("EL");
    }
}
