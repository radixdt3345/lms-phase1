namespace LMS.Application.DTOs.Approval;

/// <summary>
/// Dashboard stats for approvals — GET /api/approvals/stats.
/// </summary>
public class ApprovalStatsDto
{
    /// <summary>Number of approval actions currently waiting for the caller to act.</summary>
    public int PendingCount { get; set; }

    /// <summary>Total approvals actioned by the caller (all time).</summary>
    public int TotalActioned { get; set; }

    /// <summary>Average turnaround time in hours from creation to action, across all actioned records.</summary>
    public double AverageTurnaroundHours { get; set; }

    /// <summary>Number of requests approved by the caller this calendar year.</summary>
    public int ApprovedThisYear { get; set; }

    /// <summary>Number of requests rejected by the caller this calendar year.</summary>
    public int RejectedThisYear { get; set; }

    /// <summary>Number of requests escalated to HR by the caller this calendar year (L1 only).</summary>
    public int EscalatedThisYear { get; set; }
}
