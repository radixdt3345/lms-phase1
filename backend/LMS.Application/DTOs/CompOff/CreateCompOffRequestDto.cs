using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.CompOff;

/// <summary>
/// Payload for POST /api/comp-off-requests — FR-58, FR-59, FR-60.
/// </summary>
public class CreateCompOffRequestDto
{
    /// <summary>The date on which the employee worked. Must be a public holiday or weekend (validated in service).</summary>
    [Required]
    public DateOnly DateWorked { get; set; }

    /// <summary>Shift start time on the worked date.</summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>Shift end time on the worked date.</summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>Employee-supplied description of work performed.</summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>True when the employee is requesting a half-day credit (0.5) rather than a full day (1.0).</summary>
    public bool IsHalfDay { get; set; }
}
