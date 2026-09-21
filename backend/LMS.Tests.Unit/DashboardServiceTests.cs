using FluentAssertions;
using LMS.Application.DTOs.Dashboard;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F-11 Dashboard API layer.
/// Covers UT-F11-001 through UT-F11-004.
/// </summary>
public class DashboardServiceTests
{
    private static IDashboardService BuildService() =>
        new DashboardService(NullLogger<DashboardService>.Instance);

    // ── UT-F11-001: GetTeamLeaveOverviewAsync returns valid dto ──────────────

    [Fact]
    public async Task GetTeamLeaveOverviewAsync_ReturnsValidDto()
    {
        // Arrange
        var svc = BuildService();

        // Act
        var result = await svc.GetTeamLeaveOverviewAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().BeGreaterThan(0);
        result.OnLeaveToday.Should().Be(result.EmployeesOnLeave.Count);
        result.EmployeesOnLeave.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTeamLeaveOverviewAsync_WithManagerId_ReturnsDto()
    {
        var svc = BuildService();

        var result = await svc.GetTeamLeaveOverviewAsync("MGR-001");

        result.Should().NotBeNull();
        result.PendingRequests.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── UT-F11-002: GetApprovalSummaryAsync returns valid dto ────────────────

    [Fact]
    public async Task GetApprovalSummaryAsync_ReturnsValidDto()
    {
        var svc = BuildService();

        var result = await svc.GetApprovalSummaryAsync();

        result.Should().NotBeNull();
        result.PendingL1.Should().BeGreaterThanOrEqualTo(0);
        result.PendingL2.Should().BeGreaterThanOrEqualTo(0);
        result.AvgTurnaroundHours.Should().BeGreaterThanOrEqualTo(0);
        result.ApprovedToday.Should().BeGreaterThanOrEqualTo(0);
        result.RejectedToday.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── UT-F11-003: GetCompOffSummaryAsync returns valid dto ─────────────────

    [Fact]
    public async Task GetCompOffSummaryAsync_ReturnsValidDto()
    {
        var svc = BuildService();

        var result = await svc.GetCompOffSummaryAsync();

        result.Should().NotBeNull();
        result.TotalCreditsAvailable.Should().BeGreaterThanOrEqualTo(0);
        result.CreditsExpiringThisMonth.Should().BeGreaterThanOrEqualTo(0);
        result.TotalActiveRequests.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── UT-F11-004: GetOverviewAsync composes all three sub-DTOs ─────────────

    [Fact]
    public async Task GetOverviewAsync_ReturnsCompositeDto()
    {
        var svc = BuildService();

        var result = await svc.GetOverviewAsync();

        result.Should().NotBeNull();
        result.TeamLeave.Should().NotBeNull();
        result.Approvals.Should().NotBeNull();
        result.CompOff.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverviewAsync_TeamLeaveMatchesStandaloneCall()
    {
        var svc = BuildService();

        var overview   = await svc.GetOverviewAsync();
        var teamLeave  = await svc.GetTeamLeaveOverviewAsync();

        overview.TeamLeave.TotalEmployees.Should().Be(teamLeave.TotalEmployees);
        overview.TeamLeave.OnLeaveToday.Should().Be(teamLeave.OnLeaveToday);
    }

    [Fact]
    public async Task GetOverviewAsync_ApprovalsMatchesStandaloneCall()
    {
        var svc = BuildService();

        var overview  = await svc.GetOverviewAsync();
        var approvals = await svc.GetApprovalSummaryAsync();

        overview.Approvals.PendingL1.Should().Be(approvals.PendingL1);
        overview.Approvals.PendingL2.Should().Be(approvals.PendingL2);
    }

    [Fact]
    public async Task GetOverviewAsync_CompOffMatchesStandaloneCall()
    {
        var svc = BuildService();

        var overview = await svc.GetOverviewAsync();
        var compOff  = await svc.GetCompOffSummaryAsync();

        overview.CompOff.TotalCreditsAvailable.Should().Be(compOff.TotalCreditsAvailable);
        overview.CompOff.CreditsExpiringThisMonth.Should().Be(compOff.CreditsExpiringThisMonth);
    }

    [Fact]
    public async Task GetTeamLeaveOverviewAsync_EmployeesOnLeave_HaveRequiredFields()
    {
        var svc = BuildService();

        var result = await svc.GetTeamLeaveOverviewAsync();

        foreach (var emp in result.EmployeesOnLeave)
        {
            emp.EmployeeId.Should().NotBeNullOrEmpty();
            emp.Name.Should().NotBeNullOrEmpty();
            emp.LeaveType.Should().NotBeNullOrEmpty();
            emp.EndDate.Should().BeOnOrAfter(emp.StartDate);
        }
    }

    [Fact]
    public async Task GetApprovalSummaryAsync_WrappableInApiResponse()
    {
        var svc = BuildService();
        var dto = await svc.GetApprovalSummaryAsync();

        var envelope = LMS.Domain.Common.ApiResponse<ApprovalDashboardSummaryDto>.Ok(dto);

        envelope.Should().NotBeNull();
        envelope.Data.Should().NotBeNull();
        envelope.Data.PendingL1.Should().Be(dto.PendingL1);
    }
}
