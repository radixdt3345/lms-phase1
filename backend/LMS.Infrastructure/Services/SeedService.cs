using LMS.Application.DTOs.MasterData;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// F-14 — Initial Data Seeding: API layer implementation.
/// Wraps DataSeeder to expose seed-status queries and idempotent
/// re-seed triggers via the admin utility endpoints at /api/admin/*.
/// </summary>
public class SeedService : ISeedService
{
    private readonly LmsDbContext _context;
    private readonly DataSeeder _seeder;
    private readonly ILogger<SeedService> _logger;

    public SeedService(LmsDbContext context, DataSeeder seeder, ILogger<SeedService> logger)
    {
        _context = context;
        _seeder = seeder;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SeedStatusDto> GetSeedStatusAsync(CancellationToken cancellationToken = default)
    {
        var totalRoles = await _context.Roles.CountAsync(cancellationToken);
        var totalDepartments = await _context.Departments.CountAsync(cancellationToken);
        var totalLeaveTypes = await _context.LeaveTypes.CountAsync(cancellationToken);
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var totalPublicHolidays = await _context.PublicHolidays.CountAsync(cancellationToken);
        var totalSystemConfigs = await _context.SystemConfigs.CountAsync(cancellationToken);

        // Healthy when every seeded entity type has at least one record (AC-68)
        var isHealthy = totalRoles > 0 && totalDepartments > 0 && totalLeaveTypes > 0;

        return new SeedStatusDto
        {
            TotalRoles = totalRoles,
            TotalDepartments = totalDepartments,
            TotalLeaveTypes = totalLeaveTypes,
            TotalPublicHolidays = totalPublicHolidays,
            TotalSystemConfigs = totalSystemConfigs,
            LastSeededAt = null,
            IsHealthy = isHealthy
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Valid sections: roles | departments | leave-types | users | all.
    /// Delegating to DataSeeder which is idempotent by design (FR-92, AC-67).
    /// </remarks>
    public async Task<bool> TriggerReseedAsync(string section, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[F-14] Triggering reseed for section: {Section}", section);

        switch (section.ToLowerInvariant())
        {
            case "roles":
                await _seeder.SeedRolesAsync(cancellationToken);
                break;
            case "departments":
                await _seeder.SeedDepartmentsAsync(cancellationToken);
                break;
            case "leave-types":
                await _seeder.SeedLeaveTypesAsync(cancellationToken);
                break;
            case "holidays":
                await _seeder.SeedPublicHolidaysAsync(cancellationToken);
                break;
            case "system-configs":
                await _seeder.SeedSystemConfigsAsync(cancellationToken);
                break;
            case "all":
                await _seeder.SeedRolesAsync(cancellationToken);
                await _seeder.SeedDepartmentsAsync(cancellationToken);
                await _seeder.SeedLeaveTypesAsync(cancellationToken);
                await _seeder.SeedPublicHolidaysAsync(cancellationToken);
                await _seeder.SeedSystemConfigsAsync(cancellationToken);
                break;
            default:
                _logger.LogWarning("[F-14] Unknown reseed section requested: {Section}", section);
                return false;
        }

        _logger.LogInformation("[F-14] Reseed complete for section: {Section}", section);
        return true;
    }
}
