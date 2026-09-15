using FluentAssertions;
using LMS.Application.DTOs.Department;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F03-API-001 — Department Management API layer.
/// Covers UT-F03-API-001 through UT-F03-API-004.
/// </summary>
public class DepartmentServiceTests
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

    private static IDepartmentService BuildService(LmsDbContext context) =>
        new DepartmentService(context, NullLogger<DepartmentService>.Instance);

    private static async Task<Department> SeedDepartment(
        LmsDbContext context,
        string name = "Engineering",
        string code = "ENG",
        DateTime? deletedAt = null)
    {
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            OverlapLimit = 3,
            IsActive = deletedAt == null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = deletedAt
        };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();
        return dept;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F03-API-001: GetAllAsync returns only non-deleted departments
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyNonDeletedDepartments()
    {
        // Arrange
        var dbName = nameof(GetAllAsync_ReturnsOnlyNonDeletedDepartments);
        using var context = CreateContext(dbName);

        await SeedDepartment(context, "Engineering", "ENG");
        await SeedDepartment(context, "Human Resources", "HR");
        await SeedDepartment(context, "Deleted Dept", "DEL", deletedAt: DateTime.UtcNow.AddDays(-1));

        var svc = BuildService(context);

        // Act
        var result = (await svc.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(d => d.Code == "DEL");
        result.Select(d => d.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenAllDeleted()
    {
        var dbName = nameof(GetAllAsync_ReturnsEmpty_WhenAllDeleted);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Deleted", "DEL", deletedAt: DateTime.UtcNow);

        var result = await BuildService(context).GetAllAsync();

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F03-API-002: CreateAsync enforces unique Code
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperation_WhenCodeAlreadyExists()
    {
        // Arrange
        var dbName = nameof(CreateAsync_ThrowsInvalidOperation_WhenCodeAlreadyExists);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Engineering", "ENG");

        var svc = BuildService(context);
        var dto = new CreateDepartmentDto { Name = "Another Eng", Code = "ENG", OverlapLimit = 2 };

        // Act & Assert
        await svc.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ENG*");
    }

    [Fact]
    public async Task CreateAsync_IsCaseInsensitive_WhenCheckingCodeUniqueness()
    {
        var dbName = nameof(CreateAsync_IsCaseInsensitive_WhenCheckingCodeUniqueness);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Engineering", "ENG");

        var svc = BuildService(context);
        var dto = new CreateDepartmentDto { Name = "Another", Code = "eng" }; // lowercase

        await svc.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WithUniqueCode_AndSetsIsActiveTrue()
    {
        var dbName = nameof(CreateAsync_Succeeds_WithUniqueCode_AndSetsIsActiveTrue);
        using var context = CreateContext(dbName);
        var svc = BuildService(context);
        var dto = new CreateDepartmentDto { Name = "Finance", Code = "FIN", OverlapLimit = 5 };

        var result = await svc.CreateAsync(dto);

        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        result.Code.Should().Be("FIN");
        result.OverlapLimit.Should().Be(5);

        var saved = await context.Departments.FirstAsync(d => d.Id == result.Id);
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_AllowsCodeReuseAfterSoftDelete()
    {
        // A deleted department's code should be reusable
        var dbName = nameof(CreateAsync_AllowsCodeReuseAfterSoftDelete);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Old Eng", "ENG", deletedAt: DateTime.UtcNow);

        var svc = BuildService(context);
        var dto = new CreateDepartmentDto { Name = "New Engineering", Code = "ENG" };

        var result = await svc.CreateAsync(dto);
        result.Should().NotBeNull();
        result.Code.Should().Be("ENG");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F03-API-003: SoftDeleteAsync sets DeletedAt (does NOT hard delete)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteAsync_SetsDeletedAt_AndDoesNotHardDelete()
    {
        // Arrange
        var dbName = nameof(SoftDeleteAsync_SetsDeletedAt_AndDoesNotHardDelete);
        using var context = CreateContext(dbName);
        var dept = await SeedDepartment(context, "Sales", "SAL");

        var svc = BuildService(context);

        // Act
        var result = await svc.SoftDeleteAsync(dept.Id);

        // Assert — row still exists in DB, not hard-deleted
        result.Should().BeTrue();
        var row = await context.Departments.FindAsync(dept.Id);
        row.Should().NotBeNull();
        row!.DeletedAt.Should().NotBeNull();
        row.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_ReturnsFalse_WhenDepartmentNotFound()
    {
        var dbName = nameof(SoftDeleteAsync_ReturnsFalse_WhenDepartmentNotFound);
        using var context = CreateContext(dbName);
        var svc = BuildService(context);

        var result = await svc.SoftDeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_ReturnsFalse_WhenAlreadySoftDeleted()
    {
        var dbName = nameof(SoftDeleteAsync_ReturnsFalse_WhenAlreadySoftDeleted);
        using var context = CreateContext(dbName);
        var dept = await SeedDepartment(context, "Old Dept", "OLD", deletedAt: DateTime.UtcNow.AddDays(-5));
        var svc = BuildService(context);

        var result = await svc.SoftDeleteAsync(dept.Id);

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UT-F03-API-004: Controller endpoints return ApiResponse<T> envelope
    // (validated here via service return shape — controller delegates directly)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsDepartmentDtoCollection_WrappableInApiResponse()
    {
        var dbName = nameof(GetAllAsync_ReturnsDepartmentDtoCollection_WrappableInApiResponse);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Marketing", "MKT");
        var svc = BuildService(context);

        var items = await svc.GetAllAsync();

        // Wrap exactly as the controller does
        var envelope = LMS.Domain.Common.ApiResponse<IEnumerable<DepartmentDto>>.Ok(items);
        envelope.Should().NotBeNull();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDepartmentDto_WrappableInApiResponse()
    {
        var dbName = nameof(GetByIdAsync_ReturnsDepartmentDto_WrappableInApiResponse);
        using var context = CreateContext(dbName);
        var dept = await SeedDepartment(context, "Operations", "OPS");
        var svc = BuildService(context);

        var item = await svc.GetByIdAsync(dept.Id);

        var envelope = LMS.Domain.Common.ApiResponse<DepartmentDto>.Ok(item!);
        envelope.Should().NotBeNull();
        envelope.Data.Id.Should().Be(dept.Id);
        envelope.Data.Code.Should().Be("OPS");
    }

    [Fact]
    public async Task CreateAsync_ReturnsDepartmentDto_WrappableInApiResponse()
    {
        var dbName = nameof(CreateAsync_ReturnsDepartmentDto_WrappableInApiResponse);
        using var context = CreateContext(dbName);
        var svc = BuildService(context);
        var dto = new CreateDepartmentDto { Name = "Legal", Code = "LEG", OverlapLimit = 2 };

        var item = await svc.CreateAsync(dto);

        var envelope = LMS.Domain.Common.ApiResponse<DepartmentDto>.Ok(item);
        envelope.Should().NotBeNull();
        envelope.Data.Name.Should().Be("Legal");
    }

    [Fact]
    public async Task SoftDeleteAsync_ReturnsBool_WrappableInApiResponse()
    {
        var dbName = nameof(SoftDeleteAsync_ReturnsBool_WrappableInApiResponse);
        using var context = CreateContext(dbName);
        var dept = await SeedDepartment(context, "Temp", "TMP");
        var svc = BuildService(context);

        var deleted = await svc.SoftDeleteAsync(dept.Id);

        var envelope = LMS.Domain.Common.ApiResponse<bool>.Ok(deleted);
        envelope.Should().NotBeNull();
        envelope.Data.Should().BeTrue();
    }

    // ── Additional coverage: GetByIdAsync, UpdateAsync ────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForSoftDeletedDepartment()
    {
        var dbName = nameof(GetByIdAsync_ReturnsNull_ForSoftDeletedDepartment);
        using var context = CreateContext(dbName);
        var dept = await SeedDepartment(context, "Deleted", "DEL", deletedAt: DateTime.UtcNow);
        var svc = BuildService(context);

        var result = await svc.GetByIdAsync(dept.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenDepartmentNotFound()
    {
        var dbName = nameof(UpdateAsync_Throws_WhenDepartmentNotFound);
        using var context = CreateContext(dbName);
        var svc = BuildService(context);
        var dto = new UpdateDepartmentDto { Name = "New Name" };

        await svc.Invoking(s => s.UpdateAsync(Guid.NewGuid(), dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsInvalidOperation_WhenNewCodeConflicts()
    {
        var dbName = nameof(UpdateAsync_ThrowsInvalidOperation_WhenNewCodeConflicts);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Engineering", "ENG");
        var dept2 = await SeedDepartment(context, "Finance", "FIN");
        var svc = BuildService(context);

        var dto = new UpdateDepartmentDto { Code = "ENG" }; // conflicts with first dept

        await svc.Invoking(s => s.UpdateAsync(dept2.Id, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ENG*");
    }

    [Fact]
    public async Task GetActiveCountAsync_CountsOnlyActiveNonDeletedDepartments()
    {
        var dbName = nameof(GetActiveCountAsync_CountsOnlyActiveNonDeletedDepartments);
        using var context = CreateContext(dbName);
        await SeedDepartment(context, "Dept1", "D01");                                     // active
        await SeedDepartment(context, "Dept2", "D02");                                     // active
        await SeedDepartment(context, "Dept3", "D03", deletedAt: DateTime.UtcNow);        // soft-deleted
        var svc = BuildService(context);

        var count = await svc.GetActiveCountAsync();

        count.Should().Be(2);
    }
}
