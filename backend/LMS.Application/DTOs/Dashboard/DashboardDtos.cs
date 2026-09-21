namespace LMS.Application.DTOs.Dashboard;

/// <summary>
/// DTOs for F-11 Dashboard aggregated metrics.
/// </summary>

public record EmployeeLeaveDto(
    string EmployeeId,
    string Name,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate);

public record TeamLeaveOverviewDto(
    int TotalEmployees,
    int OnLeaveToday,
    int PendingRequests,
    List<EmployeeLeaveDto> EmployeesOnLeave);

public record ApprovalDashboardSummaryDto(
    int PendingL1,
    int PendingL2,
    double AvgTurnaroundHours,
    int ApprovedToday,
    int RejectedToday);

public record CompOffSummaryDto(
    int TotalCreditsAvailable,
    int CreditsExpiringThisMonth,
    int TotalActiveRequests);

public record OverviewDashboardDto(
    TeamLeaveOverviewDto TeamLeave,
    ApprovalDashboardSummaryDto Approvals,
    CompOffSummaryDto CompOff);

