using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.Approval;

/// <summary>
/// Payload for POST /api/approvals/{leaveRequestId}/escalate — FR-67.
/// </summary>
public class EscalateRequestDto
{
    /// <summary>Optional reason provided by the manager for escalating to HR.</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
