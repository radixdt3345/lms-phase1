using LMS.Application.DTOs.Job;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Hangfire job administration — F-15 API layer.
/// Monitoring is HRAdmin/SuperAdmin; manual trigger is SuperAdmin only.
/// All responses wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/jobs")]
[Authorize(Roles = "HRAdmin,SuperAdmin")]
[Produces("application/json")]
public class JobAdminController : ControllerBase
{
    private readonly IJobSchedulerService _jobSchedulerService;
    private readonly ILogger<JobAdminController> _logger;

    public JobAdminController(IJobSchedulerService jobSchedulerService, ILogger<JobAdminController> logger)
    {
        _jobSchedulerService = jobSchedulerService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current status of all registered Hangfire recurring jobs,
    /// enriched with the most recent execution result from JobLog.
    /// Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<IEnumerable<JobStatusDto>>>> GetStatus()
    {
        var statuses = await _jobSchedulerService.GetJobStatusesAsync();
        return Ok(ApiResponse<IEnumerable<JobStatusDto>>.Ok(statuses));
    }

    /// <summary>
    /// Manually triggers a named Hangfire job immediately.
    /// Restricted to SuperAdmin only.
    /// </summary>
    [HttpPost("{jobName}/trigger")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> TriggerJob(
        string jobName,
        [FromBody] TriggerJobRequest? request = null)
    {
        var triggered = await _jobSchedulerService.TriggerJobAsync(jobName, request?.Reason);

        if (!triggered)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job not found",
                Detail = $"No registered job with name '{jobName}' exists. " +
                         "Valid values: LeaveBalanceSyncJob, CompOffExpiryJob, EmailDispatchJob, LeaveEscalationJob.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "JOB_NOT_FOUND" }
            });
        }

        _logger.LogInformation("Job '{JobName}' manually triggered by user {UserId}",
            jobName, User.FindFirst("sub")?.Value);

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
