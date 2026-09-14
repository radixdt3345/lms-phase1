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
}
