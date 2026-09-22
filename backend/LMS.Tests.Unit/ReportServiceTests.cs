using FluentAssertions;
using LMS.Application.DTOs.Report;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests.Unit;

/// <summary>
/// Unit tests for F-12 Reports &amp; CSV Export API layer.
/// Covers UT-F12-001 through UT-F12-004.
/// </summary>
public class ReportServiceTests
{
    private static IReportService BuildService() =>
        new ReportService(NullLogger<ReportService>.Instance);

    // ── UT-F12-001: RequestReportAsync creates a job with correct fields ──────

    [Fact]
    public async Task RequestReportAsync_ReturnsJobDto_WithCorrectFields()
    {
        var svc = BuildService();

        var result = await svc.RequestReportAsync("LeaveSummary", "USER-001");

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.ReportType.Should().Be("LeaveSummary");
        result.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RequestReportAsync_StubCompletesJobImmediately()
    {
        var svc = BuildService();

        var result = await svc.RequestReportAsync("CompOffSummary", "USER-002");

        result.Status.Should().Be("Completed");
        result.OutputPath.Should().NotBeNullOrEmpty();
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RequestReportAsync_ThrowsArgumentException_WhenReportTypeEmpty()
    {
        var svc = BuildService();

        await svc.Invoking(s => s.RequestReportAsync("", "USER-001"))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Report type*");
    }

    [Fact]
    public async Task RequestReportAsync_ThrowsArgumentException_WhenUserIdEmpty()
    {
        var svc = BuildService();

        await svc.Invoking(s => s.RequestReportAsync("LeaveSummary", ""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User ID*");
    }

    // ── UT-F12-002: GetUserReportJobsAsync returns only jobs for the user ─────

    [Fact]
    public async Task GetUserReportJobsAsync_ReturnsOnlyUserJobs()
    {
        var svc = BuildService();

        await svc.RequestReportAsync("LeaveSummary",   "USER-A");
        await svc.RequestReportAsync("CompOffSummary", "USER-A");
        await svc.RequestReportAsync("ApprovalHistory","USER-B");

        var userAJobs = (await svc.GetUserReportJobsAsync("USER-A")).ToList();
        var userBJobs = (await svc.GetUserReportJobsAsync("USER-B")).ToList();

        userAJobs.Should().HaveCount(2);
        userBJobs.Should().HaveCount(1);
        userAJobs.Should().OnlyContain(j => j.ReportType == "LeaveSummary" || j.ReportType == "CompOffSummary");
    }

    [Fact]
    public async Task GetUserReportJobsAsync_ReturnsEmpty_WhenNoJobsExist()
    {
        var svc = BuildService();

        var result = await svc.GetUserReportJobsAsync("USER-NOBODY");

        result.Should().BeEmpty();
    }

    // ── UT-F12-003: GetReportJobByIdAsync finds correct job ──────────────────

    [Fact]
    public async Task GetReportJobByIdAsync_ReturnsCorrectJob()
    {
        var svc = BuildService();
        var created = await svc.RequestReportAsync("ApprovalHistory", "USER-C");

        var found = await svc.GetReportJobByIdAsync(created.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
        found.ReportType.Should().Be("ApprovalHistory");
    }

    [Fact]
    public async Task GetReportJobByIdAsync_ReturnsNull_WhenJobNotFound()
    {
        var svc = BuildService();

        var result = await svc.GetReportJobByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UT-F12-004: DownloadReportAsync returns CSV bytes ────────────────────

    [Fact]
    public async Task DownloadReportAsync_ReturnsCsvBytes_WhenJobCompleted()
    {
        var svc = BuildService();
        var job = await svc.RequestReportAsync("LeaveSummary", "USER-D");

        var bytes = await svc.DownloadReportAsync(job.Id);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        csv.Should().Contain("EmployeeId");  // CSV header present
    }

    [Fact]
    public async Task DownloadReportAsync_ThrowsKeyNotFoundException_WhenJobMissing()
    {
        var svc = BuildService();

        await svc.Invoking(s => s.DownloadReportAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DownloadReportAsync_ReturnsDifferentCsv_PerReportType()
    {
        var svc = BuildService();

        var leaveJob   = await svc.RequestReportAsync("LeaveSummary",    "USER-E");
        var compOffJob = await svc.RequestReportAsync("CompOffSummary",  "USER-E");
        var approvalJob= await svc.RequestReportAsync("ApprovalHistory", "USER-E");

        var leaveBytes    = System.Text.Encoding.UTF8.GetString(await svc.DownloadReportAsync(leaveJob.Id));
        var compOffBytes  = System.Text.Encoding.UTF8.GetString(await svc.DownloadReportAsync(compOffJob.Id));
        var approvalBytes = System.Text.Encoding.UTF8.GetString(await svc.DownloadReportAsync(approvalJob.Id));

        leaveBytes.Should().Contain("LeaveType");
        compOffBytes.Should().Contain("CreditsEarned");
        approvalBytes.Should().Contain("RequestType");
    }

    [Fact]
    public async Task RequestReportAsync_WrappableInApiResponse()
    {
        var svc = BuildService();
        var dto = await svc.RequestReportAsync("LeaveSummary", "USER-F");

        var envelope = LMS.Domain.Common.ApiResponse<ReportJobDto>.Ok(dto);

        envelope.Should().NotBeNull();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Id.Should().Be(dto.Id);
    }
}
