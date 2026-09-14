using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Seed;

public class DataSeeder
{
    private readonly LmsDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(LmsDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds system roles idempotently. Safe to run multiple times.
    /// </summary>
    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        var systemRoles = new[]
        {
            new { Name = RoleNames.HRAdmin, Description = "HR Administrator - full access to employee, leave, and policy management" },
            new { Name = RoleNames.Manager, Description = "Line Manager - can approve/reject direct report leave requests" },
            new { Name = RoleNames.Employee, Description = "Standard Employee - can submit and view own leave requests" },
            new { Name = RoleNames.Director, Description = "Director - second-level approver for backdated and escalated leave requests" },
            new { Name = RoleNames.SuperAdmin, Description = "Super Administrator - system configuration and user unlock access" }
        };

        foreach (var roleData in systemRoles)
        {
            var existing = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleData.Name, cancellationToken);

            if (existing is null)
            {
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleData.Name,
                    Description = roleData.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Roles.Add(role);
                _logger.LogInformation("Seeding role: {RoleName}", roleData.Name);
            }
            else
            {
                _logger.LogDebug("Role already exists, skipping: {RoleName}", roleData.Name);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Role seeding complete. {Count} system roles ensured.", systemRoles.Length);
    }

    /// <summary>
    /// Seeds default departments idempotently (FR-19). Safe to run multiple times.
    /// Each department gets a sensible default overlap_limit (FR-26).
    /// </summary>
    public async Task SeedDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var defaultDepartments = new[]
        {
            new { Name = "Human Resources", Code = "HR",  Description = "HR Administration and People Operations", OverlapLimit = 2 },
            new { Name = "Engineering",     Code = "ENG", Description = "Software Engineering and Technical Development", OverlapLimit = 3 },
            new { Name = "Finance",         Code = "FIN", Description = "Finance, Accounting and Budgeting", OverlapLimit = 2 },
            new { Name = "Operations",      Code = "OPS", Description = "Business Operations and Process Management", OverlapLimit = 2 }
        };

        foreach (var deptData in defaultDepartments)
        {
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Code == deptData.Code, cancellationToken);

            if (existing is null)
            {
                var department = new Department
                {
                    Id = Guid.NewGuid(),
                    Name = deptData.Name,
                    Code = deptData.Code,
                    Description = deptData.Description,
                    OverlapLimit = deptData.OverlapLimit,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Departments.Add(department);
                _logger.LogInformation("Seeding department: {DeptCode} — {DeptName}", deptData.Code, deptData.Name);
            }
            else
            {
                _logger.LogDebug("Department already exists, skipping: {DeptCode}", deptData.Code);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Department seeding complete. {Count} default departments ensured.", defaultDepartments.Length);
    }
}
