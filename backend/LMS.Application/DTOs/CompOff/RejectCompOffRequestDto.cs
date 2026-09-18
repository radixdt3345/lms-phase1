using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.CompOff;

/// <summary>
/// Payload for POST /api/comp-off-requests/{id}/reject — FR-61.
/// </summary>
public class RejectCompOffRequestDto
{
    /// <summary>Mandatory rejection reason written by the approver.</summary>
    [Required]
    [MinLength(10, ErrorMessage = "Rejection reason must be at least 10 characters.")]
    [MaxLength(500)]
    public string RejectionReason { get; set; } = string.Empty;
}
