using LMS.Application.DTOs.CompOff;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements comp-off request lifecycle — F-07 (FR-58 to FR-65).
/// </summary>
public class CompOffService : ICompOffService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<CompOffService> _logger;

    public CompOffService(LmsDbContext context, ILogger<CompOffService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    public async Task<CompOffRequestDto> SubmitAsync(Guid employeeId, CreateCompOffRequestDto dto)
    {
        // FR-59: date_worked must be a weekend or public holiday
        bool isWeekend = dto.DateWorked.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        bool isPublicHoliday = false;
        if (!isWeekend)
        {
            isPublicHoliday = await _context.PublicHolidays
                .AnyAsync(h => h.Date == dto.DateWorked && h.DeletedAt == null);
        }

        if (!isWeekend && !isPublicHoliday)
        {
            throw new InvalidOperationException("NOT_A_NON_WORKING_DAY: The date worked must be a weekend or a public holiday.");
        }

        // FR-60: worked hours from start/end times
        var workedHours = (dto.EndTime - dto.StartTime).TotalHours;
        if (workedHours < 4)
        {
            throw new InvalidOperationException("INSUFFICIENT_HOURS: Minimum 4 worked hours required to earn a comp-off credit.");
        }

        decimal credit = workedHours >= 8 ? 1.0m : 0.5m;

        // Resolve the employee's reporting manager for L1 approval (FR-61)
        var employeeProfile = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(ep => ep.UserId == employeeId && ep.DeletedAt == null);
        Guid? approverId = employeeProfile?.ManagerUserId;

        var request = new CompOffRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            DateWorked = dto.DateWorked,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsHalfDay = dto.IsHalfDay,
            Description = dto.Description,
            CalculatedCredit = credit,
            Status = "Pending",
            ApproverId = approverId
        };

        _context.CompOffRequests.Add(request);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} submitted comp-off request {RequestId} for {DateWorked}",
            employeeId, request.Id, dto.DateWorked);

        return await MapToDtoAsync(request);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<CompOffRequestDto>> GetListAsync(Guid callerId, string callerRole)
    {
        IQueryable<CompOffRequest> query = _context.CompOffRequests
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt);

        if (callerRole is "HRAdmin" or "SuperAdmin")
        {
            // HRAdmin sees all
        }
        else if (callerRole == "Manager")
        {
            // Manager sees their direct reports' requests
            var directReportIds = await _context.EmployeeProfiles
                .Where(ep => ep.ManagerUserId == callerId && ep.DeletedAt == null)
                .Select(ep => ep.UserId)
                .ToListAsync();
            query = query.Where(r => directReportIds.Contains(r.EmployeeId));
        }
        else
        {
            // Employee sees own requests only
            query = query.Where(r => r.EmployeeId == callerId);
        }

        var requests = await query.ToListAsync();
        var dtos = new List<CompOffRequestDto>();
        foreach (var r in requests)
            dtos.Add(await MapToDtoAsync(r));
        return dtos;
    }

    public async Task<CompOffRequestDto?> GetByIdAsync(Guid id)
    {
        var request = await _context.CompOffRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);
        return request is null ? null : await MapToDtoAsync(request);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    public async Task<CompOffRequestDto> ApproveAsync(Guid requestId, Guid approverId, string approverRole)
    {
        var request = await _context.CompOffRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Comp-off request '{requestId}' not found.");

        if (request.Status != "Pending")
            throw new InvalidOperationException($"Cannot approve a request in '{request.Status}' status.");

        // Only assigned approver or HRAdmin may approve (FR-61)
        bool isHrAdmin = approverRole is "HRAdmin" or "SuperAdmin";
        if (!isHrAdmin && request.ApproverId != approverId)
            throw new UnauthorizedAccessException("You are not the assigned approver for this request.");

        request.Status = "Approved";
        request.ApproverId = approverId;
        request.ApprovedAt = DateTimeOffset.UtcNow;

        // FR-62: credit the employee's Comp-off LeaveBalance
        var compOffType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Name == "Comp-Off" && lt.DeletedAt == null);

        if (compOffType is not null)
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == request.EmployeeId
                                        && lb.LeaveTypeId == compOffType.Id
                                        && lb.Year == DateTimeOffset.UtcNow.Year
                                        && lb.DeletedAt == null);
            if (balance is not null)
            {
                balance.TotalDays += request.CalculatedCredit;
            }
        }

        // FR-62: create CompOffCredit with 30-day expiry
        var earnedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var credit = new CompOffCredit
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            EarnedDate = earnedDate,
            ExpiryDate = earnedDate.AddDays(30),
            Days = request.CalculatedCredit,
            Status = "Active"
        };
        _context.CompOffCredits.Add(credit);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Approver {ApproverId} approved comp-off request {RequestId}; credited {Days} days",
            approverId, requestId, request.CalculatedCredit);

        return await MapToDtoAsync(request);
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    public async Task<CompOffRequestDto> RejectAsync(Guid requestId, Guid callerId, string callerRole, string rejectionReason)
    {
        var request = await _context.CompOffRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Comp-off request '{requestId}' not found.");

        // AC-55: employee cannot cancel/reject their own request
        if (request.EmployeeId == callerId)
            throw new UnauthorizedAccessException("SELF_CANCEL_NOT_ALLOWED: Employees cannot reject their own comp-off request.");

        if (request.Status != "Pending")
            throw new InvalidOperationException($"Cannot reject a request in '{request.Status}' status.");

        bool isHrAdmin = callerRole is "HRAdmin" or "SuperAdmin";
        if (!isHrAdmin && request.ApproverId != callerId)
            throw new UnauthorizedAccessException("You are not the assigned approver for this request.");

        request.Status = "Rejected";
        request.RejectionReason = rejectionReason;
        request.ApprovedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Caller {CallerId} rejected comp-off request {RequestId}", callerId, requestId);

        return await MapToDtoAsync(request);
    }

    // ── Credits ───────────────────────────────────────────────────────────────

    public async Task<CompOffCreditDto> GetCreditsAsync(Guid employeeId)
    {
        var credits = await _context.CompOffCredits
            .Where(c => c.EmployeeId == employeeId)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();

        var employee = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == employeeId);

        return new CompOffCreditDto
        {
            EmployeeId = employeeId,
            EmployeeName = employee is null ? string.Empty : employee.DisplayName,
            ActiveDays = credits.Where(c => c.Status == "Active").Sum(c => c.Days),
            UsedDays = credits.Where(c => c.Status == "Used").Sum(c => c.Days),
            ExpiredDays = credits.Where(c => c.Status == "Expired").Sum(c => c.Days),
            Credits = credits.Select(c => new CompOffCreditDetailDto
            {
                Id = c.Id,
                EarnedDate = c.EarnedDate,
                ExpiryDate = c.ExpiryDate,
                Days = c.Days,
                Status = c.Status
            })
        };
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private async Task<CompOffRequestDto> MapToDtoAsync(CompOffRequest r)
    {
        var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == r.EmployeeId);
        User? approver = r.ApproverId.HasValue
            ? await _context.Users.FirstOrDefaultAsync(u => u.Id == r.ApproverId)
            : null;

        return new CompOffRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = employee is null ? string.Empty : employee.DisplayName,
            DateWorked = r.DateWorked,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            IsHalfDay = r.IsHalfDay,
            Description = r.Description,
            CalculatedCredit = r.CalculatedCredit,
            Status = r.Status,
            ApproverId = r.ApproverId,
            ApproverName = approver is null ? null : approver.DisplayName,
            ApprovedAt = r.ApprovedAt,
            RejectionReason = r.RejectionReason,
            CreatedAt = r.CreatedAt
        };
    }
}
