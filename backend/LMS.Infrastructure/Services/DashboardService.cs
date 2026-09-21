using LMS.Application.DTOs.Dashboard;
using LMS.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Dashboard service — F-11.
/// Returns aggregated metrics for HR Admins and Managers.
/// Uses stub data so the endpoints are functional without a live database.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ILogger<DashboardService> logger)
    {
        _logger = logger;
    }

    // ── Team Leave Overview ───────────────────────────────────────────────────

    public Task<TeamLeaveOverviewDto> GetTeamLeaveOverviewAsync(string? managerId = null)
    {
        _logger.LogInformation("GetTeamLeaveOverviewAsync called, managerId={ManagerId}", managerId);

        var employeesOnLeave = new List<EmployeeLeaveDto>
        {
            new("EMP-001", "Alice Johnson", "Annual Leave", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(3)),
            new("EMP-002", "Bob Smith",    "Sick Leave",   DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1)),
        };

        var dto = new TeamLeaveOverviewDto(
            TotalEmployees:   50,
            OnLeaveToday:     employeesOnLeave.Count,
            PendingRequests:  7,
            EmployeesOnLeave: employeesOnLeave);

        return Task.FromResult(dto);
    }

    // ── Approval Summary ─────────────────────────────────────────────────────

    public Task<ApprovalDashboardSummaryDto> GetApprovalSummaryAsync()
    {
        _logger.LogInformation("GetApprovalSummaryAsync called");

        var dto = new ApprovalDashboardSummaryDto(
            PendingL1:          5,
            PendingL2:          3,
            AvgTurnaroundHours: 18.5,
            ApprovedToday:      4,
            RejectedToday:      1);

        return Task.FromResult(dto);
    }

    // ── Comp-Off Summary ─────────────────────────────────────────────────────

    public Task<CompOffSummaryDto> GetCompOffSummaryAsync()
    {
        _logger.LogInformation("GetCompOffSummaryAsync called");

        var dto = new CompOffSummaryDto(
            TotalCreditsAvailable:    12,
            CreditsExpiringThisMonth: 3,
            TotalActiveRequests:      6);

        return Task.FromResult(dto);
    }

    // ── Overview (all in one) ────────────────────────────────────────────────

    public async Task<OverviewDashboardDto> GetOverviewAsync()
    {
        _logger.LogInformation("GetOverviewAsync called");

        var teamLeave  = await GetTeamLeaveOverviewAsync();
        var approvals  = await GetApprovalSummaryAsync();
        var compOff    = await GetCompOffSummaryAsync();

        return new OverviewDashboardDto(teamLeave, approvals, compOff);
    }
}
