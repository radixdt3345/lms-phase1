using LMS.Application.DTOs.Report;
using LMS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Report service — F-12. Creates and manages report jobs; returns CSV bytes.
/// Uses an in-memory store so the endpoints are functional without a running database.
/// In production, replace with an EF Core-backed ReportJob entity and Hangfire background job.
/// </summary>
public class ReportService : IReportService
{
    // Simple in-process job store (per-instance; stateless across restarts by design for stubs).
    private static readonly Dictionary<Guid, ReportJob> _jobs = new();
    private static readonly object _lock = new();

    private readonly ILogger<ReportService> _logger;

    public ReportService(ILogger<ReportService> logger)
    {
        _logger = logger;
    }

    // ── Request a report ─────────────────────────────────────────────────────

    public Task<ReportJobDto> RequestReportAsync(string reportType, string userId, string? filterJson = null)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Report type is required.", nameof(reportType));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));

        var job = new ReportJob
        {
            Id          = Guid.NewGuid(),
            ReportType  = reportType,
            RequestedBy = userId,
            Status      = "Pending",
            FilterJson  = filterJson,
            RequestedAt = DateTime.UtcNow
        };

        // Simulate immediate completion for stub reports
        job.Status      = "Completed";
        job.OutputPath  = $"/reports/{job.Id}.csv";
        job.CompletedAt = DateTime.UtcNow;

        lock (_lock)
        {
            _jobs[job.Id] = job;
        }

        _logger.LogInformation("Report job {JobId} created for user {UserId}, type={ReportType}",
            job.Id, userId, reportType);

        return Task.FromResult(ToDto(job));
    }

    // ── List user's jobs ─────────────────────────────────────────────────────

    public Task<IEnumerable<ReportJobDto>> GetUserReportJobsAsync(string userId)
    {
        IEnumerable<ReportJobDto> result;

        lock (_lock)
        {
            result = _jobs.Values
                .Where(j => j.RequestedBy == userId)
                .OrderByDescending(j => j.RequestedAt)
                .Select(ToDto)
                .ToList();
        }

        return Task.FromResult(result);
    }

    // ── Get by ID ────────────────────────────────────────────────────────────

    public Task<ReportJobDto?> GetReportJobByIdAsync(Guid id)
    {
        ReportJob? job;
        lock (_lock)
        {
            _jobs.TryGetValue(id, out job);
        }

        return Task.FromResult(job is null ? null : (ReportJobDto?)ToDto(job));
    }

    // ── Download CSV ─────────────────────────────────────────────────────────

    public Task<byte[]> DownloadReportAsync(Guid id)
    {
        ReportJob? job;
        lock (_lock)
        {
            _jobs.TryGetValue(id, out job);
        }

        if (job is null)
            throw new KeyNotFoundException($"Report job {id} not found.");

        if (job.Status != "Completed")
            throw new InvalidOperationException($"Report job {id} is not yet completed (status: {job.Status}).");

        var csv = BuildSampleCsv(job.ReportType);
        return Task.FromResult(Encoding.UTF8.GetBytes(csv));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ReportJobDto ToDto(ReportJob j) =>
        new(j.Id, j.ReportType, j.Status, j.OutputPath, j.RequestedAt, j.CompletedAt);

    private static string BuildSampleCsv(string reportType) => reportType switch
    {
        "LeaveSummary" =>
            "EmployeeId,EmployeeName,LeaveType,Days,Status\n" +
            "EMP-001,Alice Johnson,Annual Leave,5,Approved\n" +
            "EMP-002,Bob Smith,Sick Leave,2,Approved\n",

        "CompOffSummary" =>
            "EmployeeId,EmployeeName,CreditsEarned,CreditsUsed,Balance\n" +
            "EMP-001,Alice Johnson,3,1,2\n" +
            "EMP-003,Carol White,2,2,0\n",

        "ApprovalHistory" =>
            "RequestId,EmployeeId,RequestType,SubmittedAt,Status,ApprovedBy\n" +
            "REQ-001,EMP-001,Leave,2025-01-10,Approved,MGR-001\n" +
            "REQ-002,EMP-002,CompOff,2025-01-12,Rejected,MGR-002\n",

        _ =>
            "Column1,Column2,Column3\n" +
            $"Sample,{reportType},Data\n"
    };

    // ── Private model ────────────────────────────────────────────────────────

    private sealed class ReportJob
    {
        public Guid Id          { get; set; }
        public string ReportType  { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string Status      { get; set; } = "Pending";
        public string? FilterJson  { get; set; }
        public string? OutputPath  { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
