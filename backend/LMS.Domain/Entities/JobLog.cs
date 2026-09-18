namespace LMS.Domain.Entities;

/// <summary>
/// Application-level log for Hangfire background job executions.
/// Hangfire maintains its own internal tables (hangfire.job, hangfire.state, etc.)
/// that are auto-created by UseHangfireStorage on startup.
/// This table records business-relevant job outcomes for audit and monitoring.
/// </summary>
public class JobLog
{
    public Guid Id { get; set; }

    /// <summary>Registered Hangfire job name (e.g. "CompOffCreditExpiry", "ApprovalReminder", "LeaveBalanceLapse").</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>Hangfire-assigned job ID (string format, e.g. "42" for recurring jobs).</summary>
    public string? JobId { get; set; }

    /// <summary>Execution status. Expected values: Succeeded, Failed, Running.</summary>
    public string Status { get; set; } = JobLogStatus.Running;

    /// <summary>Timestamp when the job started executing.</summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>Error message if the job failed; null on success.</summary>
    public string? Error { get; set; }

    /// <summary>Optional structured payload (JSON) — records processed, skipped counts, etc.</summary>
    public string? ResultPayload { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Valid values for JobLog.Status.</summary>
public static class JobLogStatus
{
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}
