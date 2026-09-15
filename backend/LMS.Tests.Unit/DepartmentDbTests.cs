using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F03-DB-001 — Department Management DB schema.
/// Covers UT-DB-F03-001 through UT-DB-F03-006.
/// </summary>
public class DepartmentDbTests
{
    private static LmsDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    // UT-DB-F03-001: Department entity has all required properties with correct defaults
    [Fact]
    public void Department_Entity_HasRequiredProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dept = new Department
        {
            Id = id,
            Name = "Engineering",
            Code = "ENG",
            Description = "Software Engineering",
            OverlapLimit = 3,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dept.Id.Should().Be(id);
        dept.Name.Should().Be("Engineering");
        dept.Code.Should().Be("ENG");
        dept.Description.Should().Be("Software Engineering");
        dept.OverlapLimit.Should().Be(3);
        dept.IsActive.Should().BeTrue();
        dept.DeletedAt.Should().BeNull();
        dept.Users.Should().BeEmpty();
    }

    // UT-DB-F03-002: Department defaults — IsActive true, OverlapLimit 1, Users empty collection
    [Fact]
    public void Department_Entity_Defaults_AreCorrect()
    {
        var dept = new Department();

        dept.IsActive.Should().BeTrue();
        dept.OverlapLimit.Should().Be(1);
        dept.Users.Should().NotBeNull();
        dept.Users.Should().BeEmpty();
        dept.DeletedAt.Should().BeNull();
    }

    // UT-DB-F03-003: LmsDbContext includes Departments DbSet and builds without errors
    [Fact]
    public void LmsDbContext_IncludesDepartments_DbSet()
    {
        using var context = CreateInMemoryContext(nameof(LmsDbContext_IncludesDepartments_DbSet));

        context.Should().NotBeNull();
        context.Departments.Should().NotBeNull();
        context.Users.Should().NotBeNull();
        context.Roles.Should().NotBeNull();
        context.UserRoles.Should().NotBeNull();
    }

    // UT-DB-F03-004: DataSeeder seeds default departments idempotently
    [Fact]
    public async Task DataSeeder_SeedsDepartments_Idempotently()
    {
        using var context = CreateInMemoryContext(nameof(DataSeeder_SeedsDepartments_Idempotently));
        var logger = NullLogger<DataSeeder>.Instance;
        var seeder = new DataSeeder(context, logger);

        // Run twice — second run must not duplicate records
        await seeder.SeedDepartmentsAsync();
        await seeder.SeedDepartmentsAsync();

        var departments = await context.Departments.ToListAsync();
        departments.Should().HaveCount(4);
        departments.Select(d => d.Code).Should().Contain(new[] { "HR", "ENG", "FIN", "OPS" });

        // All seeded departments are active
        departments.Should().AllSatisfy(d => d.IsActive.Should().BeTrue());

        // No duplicates on name or code
        departments.Select(d => d.Code).Should().OnlyHaveUniqueItems();
        departments.Select(d => d.Name).Should().OnlyHaveUniqueItems();
    }

    // UT-DB-F03-005: Department soft-delete sets DeletedAt, record is not hard-deleted (FR-23)
    [Fact]
    public async Task Department_SoftDelete_SetsDeletedAt_NotHardDeleted()
    {
        using var context = CreateInMemoryContext(nameof(Department_SoftDelete_SetsDeletedAt_NotHardDeleted));

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Finance",
            Code = "FIN",
            OverlapLimit = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        // Soft-delete: set DeletedAt and deactivate
        dept.DeletedAt = DateTime.UtcNow;
        dept.IsActive = false;
        await context.SaveChangesAsync();

        // Record still exists in the database
        var allDepts = await context.Departments.ToListAsync();
        allDepts.Should().HaveCount(1);
        allDepts[0].DeletedAt.Should().NotBeNull();
        allDepts[0].IsActive.Should().BeFalse();
    }

    // UT-DB-F03-006: Department code uniqueness constraint — two departments with the same code are rejected
    [Fact]
    public async Task Department_Code_IsUnique_ViaInMemoryDb()
    {
        using var context = CreateInMemoryContext(nameof(Department_Code_IsUnique_ViaInMemoryDb));

        var dept1 = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Operations",
            Code = "OPS",
            OverlapLimit = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(dept1);
        await context.SaveChangesAsync();

        // Verify the first record is persisted correctly
        var saved = await context.Departments.FirstAsync(d => d.Code == "OPS");
        saved.Should().NotBeNull();
        saved.Code.Should().Be("OPS");
        saved.Name.Should().Be("Operations");

        // Unique constraint verification: the entity model requires Code to be unique.
        // With InMemory provider unique constraints are not enforced at the DB level,
        // so we verify the uniqueness is encoded in the configuration via the EF model.
        var entityType = context.Model.FindEntityType(typeof(Department));
        entityType.Should().NotBeNull();
        var indexes = entityType!.GetIndexes();
        indexes.Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == "Code"));
        indexes.Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == "Name"));
    }
}
