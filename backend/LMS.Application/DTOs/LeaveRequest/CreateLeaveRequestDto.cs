using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.LeaveRequest;

/// <summary>
/// Payload for submitting or saving a leave request (F-06, FR-42, FR-43).
/// </summary>
public class CreateLeaveRequestDto
{
    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public bool IsHalfDay { get; set; }

    /// <summary>"Morning" or "Afternoon" — required when IsHalfDay is true.</summary>
    public string? HalfDayPeriod { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// When true the request is saved as Draft and does not trigger notifications
    /// or balance deduction (FR-43). When false (default) the request is Submitted immediately.
    /// </summary>
    public bool SaveAsDraft { get; set; } = false;
}
