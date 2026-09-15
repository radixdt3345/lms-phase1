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
                _logger.LogInformation("Seeding department: {DeptCode} - {DeptName}", deptData.Code, deptData.Name);
            }
            else
            {
                _logger.LogDebug("Department already exists, skipping: {DeptCode}", deptData.Code);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Department seeding complete. {Count} default departments ensured.", defaultDepartments.Length);
    }

    /// <summary>
    /// Seeds the 5 default leave types required by AC-28 and their associated default policies.
    /// Idempotent — safe to run multiple times.
    /// </summary>
    public async Task SeedLeaveTypesAsync(CancellationToken cancellationToken = default)
    {
        // AC-28: exactly these 5 leave types must be present on first deployment
        var defaultLeaveTypes = new[]
        {
            new
            {
                Name = LeaveTypeNames.CasualLeave,
                Code = LeaveTypeNames.CasualLeaveCode,
                Description = "Casual / personal leave for short-notice personal needs.",
                AnnualDays = 12,
                RequiresAttachment = false,
                RequiresHrApproval = false
            },
            new
            {
                Name = LeaveTypeNames.SickLeave,
                Code = LeaveTypeNames.SickLeaveCode,
                Description = "Medical leave for illness or injury. Medical certificate required for 3+ consecutive days.",
                AnnualDays = 6,
                RequiresAttachment = true,
                RequiresHrApproval = true
            },
            new
            {
                Name = LeaveTypeNames.EarnedLeave,
                Code = LeaveTypeNames.EarnedLeaveCode,
                Description = "Earned / privileged leave accrued through service. 1 day granted per year.",
                AnnualDays = 1,
                RequiresAttachment = false,
                RequiresHrApproval = false
            },
            new
            {
                Name = LeaveTypeNames.CompOff,
                Code = LeaveTypeNames.CompOffCode,
                Description = "Compensatory off granted for working on a holiday or weekend.",
                AnnualDays = 0,
                RequiresAttachment = false,
                RequiresHrApproval = false
            },
            new
            {
                Name = LeaveTypeNames.UnpaidLeave,
                Code = LeaveTypeNames.UnpaidLeaveCode,
                Description = "Leave without pay, taken when paid leave balance is exhausted.",
                AnnualDays = 0,
                RequiresAttachment = false,
                RequiresHrApproval = false
            }
        };

        var now = DateTime.UtcNow;
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var ltData in defaultLeaveTypes)
        {
            var existing = await _context.LeaveTypes
                .FirstOrDefaultAsync(lt => lt.Code == ltData.Code, cancellationToken);

            if (existing is null)
            {
                var leaveType = new LeaveType
                {
                    Id = Guid.NewGuid(),
                    Name = ltData.Name,
                    Code = ltData.Code,
                    Description = ltData.Description,
                    AnnualDays = ltData.AnnualDays,
                    RequiresAttachment = ltData.RequiresAttachment,
                    RequiresHrApproval = ltData.RequiresHrApproval,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.LeaveTypes.Add(leaveType);

                // Seed a default policy for each leave type
                var policy = new LeavePolicy
                {
                    Id = Guid.NewGuid(),
                    Name = $"Default Policy - {ltData.Name}",
                    LeaveTypeId = leaveType.Id,
                    LeaveType = leaveType,
                    ApplicableToRole = null,
                    AnnualAllotment = ltData.AnnualDays,
                    MaxCarryForward = 0,
                    MaxConsecutiveDays = 30,
                    MinNoticeDays = ltData.Code == LeaveTypeNames.CasualLeaveCode ? 0 : 1,
                    AccrualMonthly = false,
                    AccrualRate = null,
                    IsActive = true,
                    EffectiveFrom = yearStart,
                    EffectiveTo = null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.LeavePolicies.Add(policy);
                _logger.LogInformation("Seeding leave type: {LeaveTypeName}", ltData.Name);
            }
            else
            {
                _logger.LogDebug("Leave type already exists, skipping: {LeaveTypeName}", ltData.Name);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Leave type seeding complete. {Count} default leave types ensured.", defaultLeaveTypes.Length);
    }

    /// <summary>
    /// Seeds 2026 Indian national public holidays idempotently (FR-76, FR-91).
    /// Safe to run multiple times — checks by Name + Year + CountryCode before inserting.
    /// </summary>
    public async Task SeedPublicHolidaysAsync(CancellationToken cancellationToken = default)
    {
        const string countryCode = "IN";
        const int year = 2026;

        var holidays2026India = new[]
        {
            new { Name = "Republic Day",        Date = new DateOnly(2026, 1, 26),  IsOptional = false },
            new { Name = "Holi",                Date = new DateOnly(2026, 3, 17),  IsOptional = false },
            new { Name = "Eid ul-Fitr",         Date = new DateOnly(2026, 3, 31),  IsOptional = false },
            new { Name = "Good Friday",         Date = new DateOnly(2026, 4, 3),   IsOptional = false },
            new { Name = "Ram Navami",          Date = new DateOnly(2026, 4, 6),   IsOptional = false },
            new { Name = "Ambedkar Jayanti",    Date = new DateOnly(2026, 4, 14),  IsOptional = false },
            new { Name = "Labour Day",          Date = new DateOnly(2026, 5, 1),   IsOptional = false },
            new { Name = "Eid ul-Adha",         Date = new DateOnly(2026, 6, 7),   IsOptional = false },
            new { Name = "Independence Day",    Date = new DateOnly(2026, 8, 15),  IsOptional = false },
            new { Name = "Gandhi Jayanti",      Date = new DateOnly(2026, 10, 2),  IsOptional = false },
            new { Name = "Dussehra",            Date = new DateOnly(2026, 10, 20), IsOptional = false },
            new { Name = "Diwali",              Date = new DateOnly(2026, 11, 9),  IsOptional = false },
            new { Name = "Guru Nanak Jayanti",  Date = new DateOnly(2026, 11, 23), IsOptional = false },
            new { Name = "Christmas Day",       Date = new DateOnly(2026, 12, 25), IsOptional = false }
        };

        foreach (var holidayData in holidays2026India)
        {
            var existing = await _context.PublicHolidays
                .FirstOrDefaultAsync(
                    h => h.Name == holidayData.Name && h.Year == year && h.CountryCode == countryCode,
                    cancellationToken);

            if (existing is null)
            {
                var holiday = new PublicHoliday
                {
                    Id = Guid.NewGuid(),
                    Name = holidayData.Name,
                    Date = holidayData.Date,
                    Year = year,
                    CountryCode = countryCode,
                    IsOptional = holidayData.IsOptional,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PublicHolidays.Add(holiday);
                _logger.LogInformation("Seeding public holiday: {HolidayName} ({Date})", holidayData.Name, holidayData.Date);
            }
            else
            {
                _logger.LogDebug("Public holiday already exists, skipping: {HolidayName}", holidayData.Name);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Public holiday seeding complete. {Count} 2026 Indian holidays ensured.", holidays2026India.Length);
    }

    /// <summary>
    /// Seeds default system configuration values. Idempotent — skips keys that already exist.
    /// </summary>
    public async Task SeedSystemConfigsAsync(CancellationToken cancellationToken = default)
    {
        var defaultConfigs = new[]
        {
            new { Key = "LeaveYearStartMonth",           Value = "1",                                            Description = "Month number (1-12) when the leave year begins. 1 = January.", IsEditable = true },
            new { Key = "MaxLeaveDaysPerApplication",    Value = "30",                                           Description = "Maximum number of days allowed in a single leave application.", IsEditable = true },
            new { Key = "DefaultCountryCode",            Value = "IN",                                           Description = "ISO 3166-1 alpha-2 country code used as the default for public holidays.", IsEditable = true },
            new { Key = "WorkWeekDays",                  Value = "Monday,Tuesday,Wednesday,Thursday,Friday",     Description = "Comma-separated list of working days in the week.", IsEditable = true }
        };

        var now = DateTimeOffset.UtcNow;

        foreach (var configData in defaultConfigs)
        {
            var existing = await _context.SystemConfigs
                .FirstOrDefaultAsync(c => c.Key == configData.Key, cancellationToken);

            if (existing is null)
            {
                var config = new SystemConfig
                {
                    Id = Guid.NewGuid(),
                    Key = configData.Key,
                    Value = configData.Value,
                    Description = configData.Description,
                    IsEditable = configData.IsEditable,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.SystemConfigs.Add(config);
                _logger.LogInformation("Seeding system config: {ConfigKey}", configData.Key);
            }
            else
            {
                _logger.LogDebug("System config already exists, skipping: {ConfigKey}", configData.Key);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("System config seeding complete. {Count} default configs ensured.", defaultConfigs.Length);
    }
}
