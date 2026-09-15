using LMS.Application.DTOs.MasterData;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

public class MasterDataService : IMasterDataService
{
    private readonly LmsDbContext _context;
    private readonly DataSeeder _seeder;
    private readonly ILogger<MasterDataService> _logger;

    public MasterDataService(LmsDbContext context, DataSeeder seeder, ILogger<MasterDataService> logger)
    {
        _context = context;
        _seeder = seeder;
        _logger = logger;
    }

    public async Task<SeedStatusDto> GetSeedStatusAsync(CancellationToken cancellationToken = default)
    {
        var totalRoles = await _context.Roles.CountAsync(cancellationToken);
        var totalDepartments = await _context.Departments.CountAsync(cancellationToken);
        var totalLeaveTypes = await _context.LeaveTypes.CountAsync(cancellationToken);
        var totalPublicHolidays = await _context.PublicHolidays.CountAsync(cancellationToken);
        var totalSystemConfigs = await _context.SystemConfigs.CountAsync(cancellationToken);

        var isHealthy = totalRoles > 0 && totalDepartments > 0 && totalLeaveTypes > 0 && totalPublicHolidays > 0;

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

    public async Task<bool> TriggerReseedAsync(string section, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering reseed for section: {Section}", section);

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
                _logger.LogWarning("Unknown reseed section requested: {Section}", section);
                return false;
        }

        return true;
    }

    public async Task<IEnumerable<SystemConfigDto>> GetSystemConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _context.SystemConfigs
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);

        return configs.Select(c => new SystemConfigDto
        {
            Id = c.Id,
            Key = c.Key,
            Value = c.Value,
            Description = c.Description,
            IsEditable = c.IsEditable
        });
    }

    public async Task<SystemConfigDto?> GetSystemConfigAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);

        if (config is null) return null;

        return new SystemConfigDto
        {
            Id = config.Id,
            Key = config.Key,
            Value = config.Value,
            Description = config.Description,
            IsEditable = config.IsEditable
        };
    }

    public async Task<SystemConfigDto> UpdateSystemConfigAsync(string key, UpdateSystemConfigDto dto, CancellationToken cancellationToken = default)
    {
        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken)
            ?? throw new KeyNotFoundException($"System config key '{key}' not found.");

        if (!config.IsEditable)
            throw new InvalidOperationException($"System config key '{key}' is not editable.");

        config.Value = dto.Value;
        config.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new SystemConfigDto
        {
            Id = config.Id,
            Key = config.Key,
            Value = config.Value,
            Description = config.Description,
            IsEditable = config.IsEditable
        };
    }
}
