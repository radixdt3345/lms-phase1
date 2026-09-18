namespace LMS.Application.DTOs.LeaveRequest;

/// <summary>
/// Read model returned by all leave request endpoints (F-06).
/// </summary>
public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public string LeaveTypeCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string? HalfDayPeriod { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status: Draft, Submitted, WaitingL1, ManagerApproved,
    /// WaitingL2, FullyApproved, Active, Completed, Rejected, Cancelled, Revoked.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Working days calculated after applying sandwich rule and half-day flag.</summary>
    public decimal CalculatedDays { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
