using LMS.Application.DTOs.Report;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Api.Controllers;

/// <summary>
/// Reports &amp; CSV Export — F-12.
/// All JSON responses are wrapped in ApiResponse&lt;T&gt;.
/// The download endpoint returns a raw CSV file (text/csv).
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a report job for the specified report type.
    /// Returns the new job record wrapped in ApiResponse.
    /// </summary>
    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<ReportJobDto>>> RequestReport(
        [FromBody] CreateReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var filterJson = (request.StartDate is not null || request.EndDate is not null || request.DepartmentId is not null)
            ? System.Text.Json.JsonSerializer.Serialize(new
                {
                    startDate    = request.StartDate,
                    endDate      = request.EndDate,
                    departmentId = request.DepartmentId
                })
            : null;

        var job = await _reportService.RequestReportAsync(request.ReportType, userId, filterJson);
        return Ok(ApiResponse<ReportJobDto>.Ok(job));
    }

    /// <summary>
    /// Returns all report jobs submitted by the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReportJobDto>>>> GetUserReportJobs()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var jobs = await _reportService.GetUserReportJobsAsync(userId);
        return Ok(ApiResponse<IEnumerable<ReportJobDto>>.Ok(jobs));
    }

    /// <summary>
    /// Returns a specific report job by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportJobDto>>> GetReportJob(Guid id)
    {
        var job = await _reportService.GetReportJobByIdAsync(id);
        if (job is null)
            return NotFound(new ProblemDetails
            {
                Title  = "Report job not found",
                Detail = $"No report job with id {id}.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "REPORT_JOB_NOT_FOUND" }
            });

        return Ok(ApiResponse<ReportJobDto>.Ok(job));
    }

    /// <summary>
    /// Downloads the completed report as a CSV file.
    /// Returns 409 if the job is not yet completed.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [Produces("text/csv")]
    public async Task<IActionResult> DownloadReport(Guid id)
    {
        try
        {
            var csvBytes = await _reportService.DownloadReportAsync(id);
            return File(csvBytes, "text/csv", $"report-{id}.csv");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "DownloadReport — job {JobId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title  = "Report job not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "REPORT_JOB_NOT_FOUND" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "DownloadReport — job {JobId} not ready", id);
            return Conflict(new ProblemDetails
            {
                Title  = "Report not ready",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["error_code"] = "REPORT_NOT_COMPLETED" }
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? GetCurrentUserId() =>
        User.FindFirstValue("oid")
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
}
