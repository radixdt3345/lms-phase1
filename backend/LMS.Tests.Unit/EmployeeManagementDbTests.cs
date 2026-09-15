using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F02-DB-001 — Employee Management DB schema.
/// Covers UT-F02-DB-001, UT-F02-DB-001b, UT-F02-DB-001c.
/// </summary>
public class EmployeeManagementDbTests
{
    private static LmsDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new LmsDbContext(options);
    }

    private static User CreateUser(Guid? id = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            AzureAdObjectId = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // UT-F02-DB-001: Adding EmployeeProfile with soft-delete; query excludes deleted records
    [Fact]
    public async Task EmployeeProfile_SoftDelete_ExcludedByQueryFilter()
    {
        using var context = CreateInMemoryContext(nameof(EmployeeProfile_SoftDelete_ExcludedByQueryFilter));

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EmployeeCode = "EMP-001",
            JobTitle = "Software Engineer",
            JoiningDate = new DateOnly(2024, 1, 15),
            EmploymentType = "Full-Time",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.EmployeeProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Verify the profile is retrievable before soft-delete
        var found = await context.EmployeeProfiles.FirstOrDefaultAsync(e => e.Id == profile.Id);
        found.Should().NotBeNull();
        found!.EmployeeCode.Should().Be("EMP-001");
        found.JobTitle.Should().Be("Software Engineer");
        found.DeletedAt.Should().BeNull();

        // Soft-delete the profile
        found.DeletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        // Query filter (DeletedAt == null) should exclude the record
        var afterDelete = await context.EmployeeProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == profile.Id);
        afterDelete.Should().NotBeNull();
        afterDelete!.DeletedAt.Should().NotBeNull();

        // Via normal query, the record is excluded
        var normalQuery = await context.EmployeeProfiles.ToListAsync();
        normalQuery.Should().BeEmpty();
    }

    // UT-F02-DB-001: EmployeeProfile entity has all required fields and defaults
    [Fact]
    public void EmployeeProfile_Entity_HasRequiredProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployeeCode = "EMP-100",
            JobTitle = "Senior Developer",
            JoiningDate = new DateOnly(2023, 6, 1),
            TerminationDate = null,
            EmploymentType = "Full-Time",
            ManagerUserId = null,
            DepartmentId = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        profile.UserId.Should().Be(userId);
        profile.EmployeeCode.Should().Be("EMP-100");
        profile.JobTitle.Should().Be("Senior Developer");
        profile.JoiningDate.Should().Be(new DateOnly(2023, 6, 1));
        profile.TerminationDate.Should().BeNull();
        profile.EmploymentType.Should().Be("Full-Time");
        profile.ManagerUserId.Should().BeNull();
        profile.DepartmentId.Should().BeNull();
        profile.DeletedAt.Should().BeNull();
    }

    // UT-F02-DB-001b: EmployeeLeaveBalance unique constraint on (UserId, LeaveTypeId, Year)
    [Fact]
    public async Task EmployeeLeaveBalance_UniqueConstraint_EncodedInModel()
    {
        using var context = CreateInMemoryContext(nameof(EmployeeLeaveBalance_UniqueConstraint_EncodedInModel));

        var user = CreateUser();
        context.Users.Add(user);

        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = "Annual Leave",
            Code = "AL",
            AnnualDays = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeaveTypes.Add(leaveType);
        await context.SaveChangesAsync();

        var balance = new EmployeeLeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2026,
            TotalAllocated = 20m,
            Used = 5m,
            Carried = 2m,
            Available = 17m,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.EmployeeLeaveBalances.Add(balance);
        await context.SaveChangesAsync();

        // Verify the balance is persisted correctly
        var saved = await context.EmployeeLeaveBalances.FirstAsync(b => b.UserId == user.Id);
        saved.TotalAllocated.Should().Be(20m);
        saved.Used.Should().Be(5m);
        saved.Carried.Should().Be(2m);
        saved.Available.Should().Be(17m);
        saved.Year.Should().Be(2026);

        // Verify the unique index is encoded in the EF model
        var entityType = context.Model.FindEntityType(typeof(EmployeeLeaveBalance));
        entityType.Should().NotBeNull();
        var indexes = entityType!.GetIndexes();
        indexes.Should().Contain(i =>
            i.IsUnique &&
            i.Properties.Any(p => p.Name == "UserId") &&
            i.Properties.Any(p => p.Name == "LeaveTypeId") &&
            i.Properties.Any(p => p.Name == "Year"));
    }

    // UT-F02-DB-001b: EmployeeLeaveBalance stores correct numeric fields
    [Fact]
    public void EmployeeLeaveBalance_Entity_HasCorrectFields()
    {
        var balance = new EmployeeLeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            Year = 2026,
            TotalAllocated = 20.5m,
            Used = 3.5m,
            Carried = 5.0m,
            Available = 22.0m,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        balance.TotalAllocated.Should().Be(20.5m);
        balance.Used.Should().Be(3.5m);
        balance.Carried.Should().Be(5.0m);
        balance.Available.Should().Be(22.0m);
        balance.Year.Should().Be(2026);
    }

    // UT-F02-DB-001c: EmployeeDocument stores correct metadata fields
    [Fact]
    public async Task EmployeeDocument_StoresCorrectMetadataFields()
    {
        using var context = CreateInMemoryContext(nameof(EmployeeDocument_StoresCorrectMetadataFields));

        var user = CreateUser();
        var uploader = CreateUser();
        context.Users.AddRange(user, uploader);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var doc = new EmployeeDocument
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentType = "offer_letter",
            FileName = "offer_letter_2024.pdf",
            StoragePath = "blobs/employees/user-123/offer_letter_2024.pdf",
            FileSizeBytes = 204800L,
            ContentType = "application/pdf",
            UploadedAt = now,
            UploadedByUserId = uploader.Id,
            CreatedAt = now
        };
        context.EmployeeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var saved = await context.EmployeeDocuments.FirstAsync(d => d.Id == doc.Id);
        saved.DocumentType.Should().Be("offer_letter");
        saved.FileName.Should().Be("offer_letter_2024.pdf");
        saved.StoragePath.Should().Be("blobs/employees/user-123/offer_letter_2024.pdf");
        saved.FileSizeBytes.Should().Be(204800L);
        saved.ContentType.Should().Be("application/pdf");
        saved.UploadedByUserId.Should().Be(uploader.Id);
        saved.DeletedAt.Should().BeNull();
    }

    // UT-F02-DB-001c: EmployeeDocument soft-delete via DeletedAt
    [Fact]
    public async Task EmployeeDocument_SoftDelete_ExcludedByQueryFilter()
    {
        using var context = CreateInMemoryContext(nameof(EmployeeDocument_SoftDelete_ExcludedByQueryFilter));

        var user = CreateUser();
        var uploader = CreateUser();
        context.Users.AddRange(user, uploader);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var doc = new EmployeeDocument
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentType = "id_proof",
            FileName = "passport.jpg",
            StoragePath = "blobs/employees/user-123/passport.jpg",
            FileSizeBytes = 512000L,
            ContentType = "image/jpeg",
            UploadedAt = now,
            UploadedByUserId = uploader.Id,
            CreatedAt = now
        };
        context.EmployeeDocuments.Add(doc);
        await context.SaveChangesAsync();

        // Should appear in normal query
        var count = await context.EmployeeDocuments.CountAsync();
        count.Should().Be(1);

        // Soft-delete
        doc.DeletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        // Normal query should exclude it
        var afterDelete = await context.EmployeeDocuments.ToListAsync();
        afterDelete.Should().BeEmpty();

        // IgnoreQueryFilters should still find it
        var raw = await context.EmployeeDocuments.IgnoreQueryFilters().ToListAsync();
        raw.Should().HaveCount(1);
        raw[0].DeletedAt.Should().NotBeNull();
    }

    // Verify LmsDbContext includes all three new DbSets
    [Fact]
    public void LmsDbContext_IncludesEmployeeManagement_DbSets()
    {
        using var context = CreateInMemoryContext(nameof(LmsDbContext_IncludesEmployeeManagement_DbSets));

        context.EmployeeProfiles.Should().NotBeNull();
        context.EmployeeLeaveBalances.Should().NotBeNull();
        context.EmployeeDocuments.Should().NotBeNull();
    }
}
