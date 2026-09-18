using LMS.Application.DTOs.Approval;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements the approval workflow — F-08 (FR-66 to FR-69).
/// The two-level chain: L1 = reporting manager, L2 = HR Admin.
/// Rules:
///   - Request with no reporting manager: bypasses L1 and goes directly to WAITING_HR_APPROVAL (FR-66).
///   - After L1 approval: if duration > 3 days, leave type RequiresHrApproval, or backdated → L2 required (FR-67).
///   - Otherwise: fully approved immediately.
///   - Reminder: Hangfire job (external) sends email every 2 days; this service only exposes the pending queue (FR-69).
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(LmsDbContext context, ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Pending queue ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<ApprovalRecordDto>> GetPendingAsync(Guid callerId, string callerRole)
    {
        IQueryable<ApprovalRecord> query = _context.ApprovalRecords
            .Where(ar => ar.Action == null || ar.Action == string.Empty)
            .OrderBy(ar => ar.CreatedAt);

        if (callerRole is "HRAdmin" or "SuperAdmin")
        {
            // HR Admin sees all pending L2 records
            query = query.Where(ar => ar.Level == ApprovalLevel.L2);
        }
        else
        {
            // Manager sees their own pending L1 records
            query = query.Where(ar => ar.ApproverId == callerId && ar.Level == ApprovalLevel.L1);
        }

        var records = await query.ToListAsync();
        var dtos = new List<ApprovalRecordDto>();
        foreach (var r in records)
            dtos.Add(await MapToDtoAsync(r));
        return dtos;
    }

    // ── History ───────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ApprovalRecordDto>> GetHistoryAsync(Guid callerId, string callerRole)
    {
        IQueryable<ApprovalRecord> query = _context.ApprovalRecords
            .Where(ar => ar.Action != null && ar.Action != string.Empty)
            .OrderByDescending(ar => ar.ActedAt);

        if (callerRole is "HRAdmin" or "SuperAdmin")
        {
            // HRAdmin sees all actioned records
        }
        else
        {
            // Manager/Employee sees only records where they were the approver
            query = query.Where(ar => ar.ApproverId == callerId);
        }

        var records = await query.ToListAsync();
        var dtos = new List<ApprovalRecordDto>();
        foreach (var r in records)
            dtos.Add(await MapToDtoAsync(r));
        return dtos;
    }

    // ── Escalate ──────────────────────────────────────────────────────────────

    public async Task<ApprovalRecordDto> EscalateAsync(Guid leaveRequestId, Guid managerId, string reason)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId && lr.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Leave request '{leaveRequestId}' not found.");

        // Only manager who is the L1 approver can escalate
        var l1Record = await _context.ApprovalRecords
            .FirstOrDefaultAsync(ar => ar.LeaveRequestId == leaveRequestId
                                    && ar.Level == ApprovalLevel.L1
                                    && ar.ApproverId == managerId);

        if (l1Record is null)
            throw new UnauthorizedAccessException("You are not the L1 approver for this leave request.");

        // Request must be in a state that allows escalation (ManagerApproved or WaitingL2)
        if (leaveRequest.Status != "ManagerApproved" && leaveRequest.Status != "WaitingL2")
            throw new InvalidOperationException($"Cannot escalate a request in '{leaveRequest.Status}' status.");

        // Create L2 approval record for HR Admin
        var l2Record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequestId,
            // ApproverId for L2 is left as the generic HR queue — no specific HR user targeted here
            ApproverId = managerId, // Will be re-assigned when HR Admin acts
            Level = ApprovalLevel.L2,
            Comments = reason
        };
        _context.ApprovalRecords.Add(l2Record);

        leaveRequest.Status = "WaitingL2";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Manager {ManagerId} escalated leave request {LeaveRequestId} to L2",
            managerId, leaveRequestId);

        return await MapToDtoAsync(l2Record);
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    public async Task<ApprovalStatsDto> GetStatsAsync(Guid callerId, string callerRole)
    {
        var currentYear = DateTime.UtcNow.Year;

        IQueryable<ApprovalRecord> baseQuery = _context.ApprovalRecords;
        if (callerRole is not ("HRAdmin" or "SuperAdmin"))
        {
            baseQuery = baseQuery.Where(ar => ar.ApproverId == callerId);
        }

        var pending = await baseQuery
            .CountAsync(ar => ar.Action == null || ar.Action == string.Empty);

        var actioned = await baseQuery
            .Where(ar => ar.Action != null && ar.Action != string.Empty)
            .ToListAsync();

        double avgTurnaround = 0;
        if (actioned.Count > 0)
        {
            avgTurnaround = actioned
                .Where(ar => ar.ActedAt.HasValue)
                .Select(ar => (ar.ActedAt!.Value - ar.CreatedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average();
        }

        var approvedThisYear = actioned.Count(ar =>
            ar.Action == ApprovalAction.Approved && ar.ActedAt?.Year == currentYear);
        var rejectedThisYear = actioned.Count(ar =>
            ar.Action == ApprovalAction.Rejected && ar.ActedAt?.Year == currentYear);

        return new ApprovalStatsDto
        {
            PendingCount = pending,
            TotalActioned = actioned.Count,
            AverageTurnaroundHours = Math.Round(avgTurnaround, 1),
            ApprovedThisYear = approvedThisYear,
            RejectedThisYear = rejectedThisYear,
            EscalatedThisYear = 0 // Escalation tracking requires a dedicated audit table — placeholder
        };
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private async Task<ApprovalRecordDto> MapToDtoAsync(ApprovalRecord ar)
    {
        var approver = await _context.Users.FirstOrDefaultAsync(u => u.Id == ar.ApproverId);

        // Determine employee and request summary from the underlying request
        string employeeName = string.Empty;
        Guid employeeId = Guid.Empty;
        string requestSummary = string.Empty;
        string requestStatus = string.Empty;

        if (ar.CompOffRequestId.HasValue)
        {
            var compOff = await _context.CompOffRequests
                .FirstOrDefaultAsync(r => r.Id == ar.CompOffRequestId);
            if (compOff is not null)
            {
                employeeId = compOff.EmployeeId;
                requestStatus = compOff.Status;
                requestSummary = $"Comp-Off: {compOff.DateWorked:yyyy-MM-dd} ({compOff.CalculatedCredit} day)";
                var emp = await _context.Users.FirstOrDefaultAsync(u => u.Id == compOff.EmployeeId);
                employeeName = emp?.DisplayName ?? string.Empty;
            }
        }
        else
        {
            var leave = await _context.LeaveRequests
                .Include(r => r.LeaveType)
                .FirstOrDefaultAsync(r => r.Id == ar.LeaveRequestId);
            if (leave is not null)
            {
                employeeId = leave.EmployeeId;
                requestStatus = leave.Status;
                requestSummary = $"{leave.LeaveType?.Name ?? "Leave"}: {leave.StartDate:yyyy-MM-dd} → {leave.EndDate:yyyy-MM-dd} ({leave.CalculatedDays} days)";
                var emp = await _context.Users.FirstOrDefaultAsync(u => u.Id == leave.EmployeeId);
                employeeName = emp?.DisplayName ?? string.Empty;
            }
        }

        return new ApprovalRecordDto
        {
            Id = ar.Id,
            LeaveRequestId = ar.LeaveRequestId,
            CompOffRequestId = ar.CompOffRequestId,
            ApproverId = ar.ApproverId,
            ApproverName = approver?.DisplayName ?? string.Empty,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Level = ar.Level,
            Action = ar.Action,
            Comments = ar.Comments,
            ActedAt = ar.ActedAt,
            CreatedAt = ar.CreatedAt,
            RequestSummary = requestSummary,
            RequestStatus = requestStatus
        };
    }
}
