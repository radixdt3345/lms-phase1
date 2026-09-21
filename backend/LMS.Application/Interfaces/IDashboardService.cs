namespace LMS.Application.Interfaces;

/// <summary>
/// Dashboard service interface — F-11 (read-only aggregated metrics).
/// </summary>
public interface IDashboardService
{
    Task<TeamLeaveOverviewDto> GetTeamLeaveOverviewAsync(string? managerId = null);
    Task<ApprovalDashboardSummaryDto> GetApprovalSummaryAsync();
    Task<CompOffSummaryDto> GetCompOffSummaryAsync();
    Task<OverviewDashboardDto> GetOverviewAsync();
}
