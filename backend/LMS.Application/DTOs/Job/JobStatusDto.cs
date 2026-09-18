namespace LMS.Application.DTOs.Job;

/// <summary>
/// Summary of a registered Hangfire recurring job — returned by GET /api/jobs/status.
/// </summary>
public class JobStatusDto
{
    /// <summary>The registered recurring job ID (e.g. "LeaveBalanceSyncJob").</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Cron expression governing when this job fires.</summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the last recorded execution, or null if never run.</summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>Status of the last execution: Succeeded, Failed, Running, or null.</summary>
    public string? LastStatus { get; set; }

    /// <summary>Error message from the last execution, or null on success.</summary>
    public string? LastError { get; set; }

    /// <summary>Number of records processed in the last run (from ResultPayload), or null.</summary>
    public string? LastResultSummary { get; set; }
}

/// <summary>Request body for POST /api/jobs/{jobName}/trigger.</summary>
public class TriggerJobRequest
{
    /// <summary>Optional reason/note for the manual trigger (written to JobLog).</summary>
    public string? Reason { get; set; }
}
