using LMS.Application.DTOs.LeaveBalance;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements leave balance management — F-05 (FR-33 to FR-41).
/// All reads go directly to the database (no caching, per FR-41).
/// </summary>
public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<LeaveBalanceService> _logger;

    public LeaveBalanceService(LmsDbContext context, ILogger<LeaveBalanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<LeaveBalanceDto>> GetAllAsync()
    {
        var balances = await _context.LeaveBalances
            .Include(b => b.Employee)
            .Include(b => b.LeaveType)
            .Where(b => b.DeletedAt == null)
            .OrderBy(b => b.Employee.DisplayName)
            .ThenBy(b => b.LeaveType.Name)
            .ToListAsync();

        return balances.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaveBalanceDto>> GetByEmployeeIdAsync(Guid employeeId)
    {
        var balances = await _context.LeaveBalances
            .Include(b => b.Employee)
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.DeletedAt == null)
            .OrderBy(b => b.LeaveType.Name)
            .ToListAsync();

        return balances.Select(MapToDto);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<LeaveBalanceDto> AdjustBalanceAsync(AdjustBalanceDto dto)
    {
        var balance = await _context.LeaveBalances
            .Include(b => b.Employee)
            .Include(b => b.LeaveType)
            .FirstOrDefaultAsync(b =>
                b.EmployeeId == dto.EmployeeId &&
                b.LeaveTypeId == dto.LeaveTypeId &&
                b.Year == dto.Year &&
                b.DeletedAt == null);

        if (balance is null)
        {
            throw new KeyNotFoundException(
                $"No leave balance found for employee '{dto.EmployeeId}', " +
                $"leave type '{dto.LeaveTypeId}', year {dto.Year}.");
        }

        balance.AdjustedDays += dto.Adjustment;
        balance.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "HR adjusted balance {BalanceId}: {Adjustment:+0.5;-0.5} days — reason: {Reason}",
            balance.Id, dto.Adjustment, dto.Reason);

        return MapToDto(balance);
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    public async Task<LeaveBalanceSummaryDto> GetSummaryAsync()
    {
        var year = DateTime.UtcNow.Year;

        var balances = await _context.LeaveBalances
            .Where(b => b.Year == year && b.DeletedAt == null)
            .ToListAsync();

        var totalAllocated = balances.Sum(b => b.TotalDays + b.AdjustedDays);
        var totalUsed = balances.Sum(b => b.UsedDays);
        var totalPending = balances.Sum(b => b.PendingDays);

        return new LeaveBalanceSummaryDto
        {
            Year = year,
            TotalEmployees = balances.Select(b => b.EmployeeId).Distinct().Count(),
            TotalDaysAllocated = totalAllocated,
            TotalDaysUsed = totalUsed,
            TotalDaysPending = totalPending,
            TotalDaysAvailable = totalAllocated - totalUsed - totalPending
        };
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static LeaveBalanceDto MapToDto(LeaveBalance b) => new()
    {
        Id = b.Id,
        EmployeeId = b.EmployeeId,
        EmployeeName = b.Employee?.DisplayName ?? string.Empty,
        LeaveTypeId = b.LeaveTypeId,
        LeaveTypeName = b.LeaveType?.Name ?? string.Empty,
        LeaveTypeCode = b.LeaveType?.Code ?? string.Empty,
        Year = b.Year,
        TotalDays = b.TotalDays,
        UsedDays = b.UsedDays,
        PendingDays = b.PendingDays,
        AdjustedDays = b.AdjustedDays,
        AvailableDays = b.TotalDays + b.AdjustedDays - b.UsedDays - b.PendingDays,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
