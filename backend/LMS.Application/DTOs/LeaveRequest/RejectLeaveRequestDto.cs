using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.LeaveRequest;

/// <summary>
/// Payload for rejecting a leave request (AC-44 — reason is mandatory).
/// </summary>
public class RejectLeaveRequestDto
{
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
