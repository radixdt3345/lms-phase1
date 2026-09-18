using LMS.Application.DTOs.LeaveRequest;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements leave application and workflow — F-06 (FR-42 to FR-57, FR-70).
/// Covers the full lifecycle: Draft → Submitted → approval chain → terminal states.
/// </summary>
public class LeaveRequestService : ILeaveRequestService
{
    private static readonly HashSet<string> PendingStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Submitted", "WaitingL1", "WaitingL2"
    };

    private static readonly HashSet<string> CancellableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Submitted", "WaitingL1"
    };

    private readonly LmsDbContext _context;
    private readonly ILogger<LeaveRequestService> _logger;

    public LeaveRequestService(LmsDbContext context, ILogger<LeaveRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<LeaveRequestDto> CreateAsync(Guid employeeId, CreateLeaveRequestDto dto)
    {
        if (dto.StartDate > dto.EndDate)
        {
            throw new InvalidOperationException("StartDate must not be after EndDate.");
        }

        if (dto.IsHalfDay && string.IsNullOrWhiteSpace(dto.HalfDayPeriod))
        {
            throw new InvalidOperationException("HalfDayPeriod is required for a half-day request.");
        }

        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.IsActive && lt.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Leave type '{dto.LeaveTypeId}' not found or inactive.");

        var calculatedDays = dto.IsHalfDay ? 0.5m : (decimal)(dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1);

        var status = dto.SaveAsDraft ? "Draft" : "Submitted";

        if (status == "Submitted")
        {
            // Validate balance (FR-39) — Unpaid Leave has no limit
            if (!string.Equals(leaveType.Code, "UL", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(leaveType.Name, "Unpaid Leave", StringComparison.OrdinalIgnoreCase))
            {
                var balance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(b =>
                        b.EmployeeId == employeeId &&
                        b.LeaveTypeId == dto.LeaveTypeId &&
                        b.Year == dto.StartDate.Year &&
                        b.DeletedAt == null);

                var available = balance is null
                    ? 0m
                    : balance.TotalDays + balance.AdjustedDays - balance.UsedDays - balance.PendingDays;

                if (available < calculatedDays)
                {
                    throw new InvalidOperationException("INSUFFICIENT_BALANCE");
                }

                // Reserve pending days
                if (balance is not null)
                {
                    balance.PendingDays += calculatedDays;
                    balance.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            // Check for overlapping approved/pending requests (FR-44)
            var overlap = await _context.LeaveRequests
                .AnyAsync(lr =>
                    lr.EmployeeId == employeeId &&
                    lr.DeletedAt == null &&
                    lr.StartDate <= dto.EndDate &&
                    lr.EndDate >= dto.StartDate &&
                    (lr.Status == "FullyApproved" || lr.Status == "Active" ||
                     lr.Status == "Submitted" || lr.Status == "WaitingL1" ||
                     lr.Status == "WaitingL2" || lr.Status == "ManagerApproved"));

            if (overlap)
            {
                throw new InvalidOperationException("LEAVE_OVERLAP");
            }
        }

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsHalfDay = dto.IsHalfDay,
            HalfDayPeriod = dto.HalfDayPeriod,
            Reason = dto.Reason,
            Status = status,
            CalculatedDays = calculatedDays,
            SubmittedAt = status == "Submitted" ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created leave request {RequestId} for employee {EmployeeId} with status {Status}",
            request.Id, employeeId, status);

        return await LoadDtoAsync(request.Id);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<LeaveRequestDto>> GetAllAsync(Guid requesterId, IEnumerable<string> roles)
    {
        var roleList = roles.ToList();
        var isHrAdmin = roleList.Contains("HRAdmin", StringComparer.OrdinalIgnoreCase);
        var isManager = roleList.Contains("Manager", StringComparer.OrdinalIgnoreCase);

        IQueryable<LeaveRequest> query = _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .Where(r => r.DeletedAt == null);

        if (!isHrAdmin && !isManager)
        {
            // Employee: own requests only
            query = query.Where(r => r.EmployeeId == requesterId);
        }
        else if (isManager && !isHrAdmin)
        {
            // Manager: team requests (employees whose ManagerAzureAdObjectId matches)
            var managerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == requesterId && u.DeletedAt == null);

            if (managerUser is not null)
            {
                query = query.Where(r =>
                    r.EmployeeId == requesterId ||
                    r.Employee.ManagerAzureAdObjectId == managerUser.AzureAdObjectId);
            }
            else
            {
                query = query.Where(r => r.EmployeeId == requesterId);
            }
        }
        // HRAdmin: no additional filter — all requests

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToDto);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(Guid id)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        return request is null ? null : MapToDto(request);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetPendingForApproverAsync(Guid approverId)
    {
        // Return requests in pending states; scoping by manager is handled at the controller level.
        var requests = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .Where(r =>
                r.DeletedAt == null &&
                PendingStatuses.Contains(r.Status))
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        return requests.Select(MapToDto);
    }

    // ── Workflow ──────────────────────────────────────────────────────────────

    public async Task<LeaveRequestDto> CancelAsync(Guid id, Guid employeeId)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Leave request '{id}' not found.");

        if (request.EmployeeId != employeeId)
        {
            throw new InvalidOperationException("You can only cancel your own leave requests.");
        }

        if (!CancellableStatuses.Contains(request.Status))
        {
            throw new InvalidOperationException(
                $"Leave request cannot be cancelled from status '{request.Status}'.");
        }

        // Restore pending/used balance (FR-51)
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.EmployeeId == employeeId &&
                b.LeaveTypeId == request.LeaveTypeId &&
                b.Year == request.StartDate.Year &&
                b.DeletedAt == null);

        if (balance is not null)
        {
            if (request.Status == "FullyApproved" || request.Status == "ManagerApproved")
            {
                balance.UsedDays = Math.Max(0, balance.UsedDays - request.CalculatedDays);
            }
            else
            {
                balance.PendingDays = Math.Max(0, balance.PendingDays - request.CalculatedDays);
            }
            balance.UpdatedAt = DateTimeOffset.UtcNow;
        }

        request.Status = "Cancelled";
        request.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled leave request {RequestId}", id);
        return MapToDto(request);
    }

    public async Task<LeaveRequestDto> ApproveAsync(Guid id, Guid approverId)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Leave request '{id}' not found.");

        // Determine next status per state machine (FR-48)
        var nextStatus = request.Status switch
        {
            "Submitted" or "WaitingL1" => DeterminePostL1Status(request),
            "WaitingL2" or "ManagerApproved" => "FullyApproved",
            _ => throw new InvalidOperationException(
                $"Leave request cannot be approved from status '{request.Status}'.")
        };

        // Move pending days to used when fully approved
        if (nextStatus == "FullyApproved")
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.EmployeeId == request.EmployeeId &&
                    b.LeaveTypeId == request.LeaveTypeId &&
                    b.Year == request.StartDate.Year &&
                    b.DeletedAt == null);

            if (balance is not null)
            {
                balance.PendingDays = Math.Max(0, balance.PendingDays - request.CalculatedDays);
                balance.UsedDays += request.CalculatedDays;
                balance.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        request.Status = nextStatus;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Approved leave request {RequestId} by {ApproverId} — new status: {Status}",
            id, approverId, nextStatus);

        return MapToDto(request);
    }

    public async Task<LeaveRequestDto> RejectAsync(Guid id, Guid approverId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("REJECTION_REASON_REQUIRED");
        }

        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Leave request '{id}' not found.");

        if (!PendingStatuses.Contains(request.Status) && request.Status != "ManagerApproved")
        {
            throw new InvalidOperationException(
                $"Leave request cannot be rejected from status '{request.Status}'.");
        }

        // Restore balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.EmployeeId == request.EmployeeId &&
                b.LeaveTypeId == request.LeaveTypeId &&
                b.Year == request.StartDate.Year &&
                b.DeletedAt == null);

        if (balance is not null)
        {
            balance.PendingDays = Math.Max(0, balance.PendingDays - request.CalculatedDays);
            balance.UpdatedAt = DateTimeOffset.UtcNow;
        }

        request.Status = "Rejected";
        request.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Rejected leave request {RequestId} by {ApproverId} — reason: {Reason}",
            id, approverId, reason);

        return MapToDto(request);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Determines the status after L1 approval based on duration and leave type policy (FR-48, AC-42/43).
    /// Backdated requests always go to WaitingL2 (FR-50).
    /// </summary>
    private static string DeterminePostL1Status(LeaveRequest request)
    {
        var isBackdated = request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (isBackdated)
            return "WaitingL2";

        // Duration > 3 days or HR approval required by leave type → WaitingL2
        if (request.CalculatedDays > 3 || (request.LeaveType?.RequiresHrApproval ?? false))
            return "WaitingL2";

        return "FullyApproved";
    }

    private async Task<LeaveRequestDto> LoadDtoAsync(Guid id)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .FirstAsync(r => r.Id == id);

        return MapToDto(request);
    }

    private static LeaveRequestDto MapToDto(LeaveRequest r) => new()
    {
        Id = r.Id,
        EmployeeId = r.EmployeeId,
        EmployeeName = r.Employee?.DisplayName ?? string.Empty,
        LeaveTypeId = r.LeaveTypeId,
        LeaveTypeName = r.LeaveType?.Name ?? string.Empty,
        LeaveTypeCode = r.LeaveType?.Code ?? string.Empty,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        IsHalfDay = r.IsHalfDay,
        HalfDayPeriod = r.HalfDayPeriod,
        Reason = r.Reason,
        Status = r.Status,
        CalculatedDays = r.CalculatedDays,
        SubmittedAt = r.SubmittedAt,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
