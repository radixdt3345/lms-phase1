using LMS.Application.DTOs.Dashboard;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Read-only management dashboards — F-11 (FR for HR Admins and Managers).
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/dashboards")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Returns team leave overview — employees on leave today and pending requests.
    /// Restricted to Manager, HRAdmin, and SuperAdmin.
    /// </summary>
    [HttpGet("team-leave")]
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<TeamLeaveOverviewDto>>> GetTeamLeaveOverview(
        [FromQuery] string? managerId = null)
    {
        var result = await _dashboardService.GetTeamLeaveOverviewAsync(managerId);
        return Ok(ApiResponse<TeamLeaveOverviewDto>.Ok(result));
    }

    /// <summary>
    /// Returns approval queue metrics — pending counts and average turnaround.
    /// Restricted to Manager, HRAdmin, and SuperAdmin.
    /// </summary>
    [HttpGet("approvals")]
    [Authorize(Roles = "Manager,HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<ApprovalDashboardSummaryDto>>> GetApprovalSummary()
    {
        var result = await _dashboardService.GetApprovalSummaryAsync();
        return Ok(ApiResponse<ApprovalDashboardSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Returns comp-off summary — credits available and expiring this month.
    /// Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpGet("comp-off")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<CompOffSummaryDto>>> GetCompOffSummary()
    {
        var result = await _dashboardService.GetCompOffSummaryAsync();
        return Ok(ApiResponse<CompOffSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Returns all dashboard metrics in a single response.
    /// Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpGet("overview")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<OverviewDashboardDto>>> GetOverview()
    {
        var result = await _dashboardService.GetOverviewAsync();
        return Ok(ApiResponse<OverviewDashboardDto>.Ok(result));
    }
}
