using LMS.Application.DTOs.Job;

namespace LMS.Application.Interfaces;

/// <summary>
/// Hangfire recurring job registration and monitoring — F-15 API layer.
/// </summary>
public interface IJobSchedulerService
{
    /// <summary>
    /// Registers all LMS recurring Hangfire jobs. Called once from Program.cs on startup.
    /// Idempotent — safe to call multiple times; Hangfire's AddOrUpdate overwrites existing registrations.
    /// </summary>
    void RegisterJobs();

    /// <summary>Returns the status of all registered recurring jobs, enriched from JobLog.</summary>
    Task<IEnumerable<JobStatusDto>> GetJobStatusesAsync();

    /// <summary>
    /// Manually triggers a named job immediately (enqueues a fire-and-forget Hangfire job).
    /// Returns false if the job name is not recognised.
    /// </summary>
    Task<bool> TriggerJobAsync(string jobName, string? reason = null);

    // --- Job runner methods — called by Hangfire via generic expression lambdas ---
    // These must be on the interface so Hangfire can resolve them through DI.
    Task RunLeaveBalanceSyncJobAsync();
    Task RunCompOffExpiryJobAsync();
    Task RunEmailDispatchJobAsync();
    Task RunLeaveEscalationJobAsync();
}
