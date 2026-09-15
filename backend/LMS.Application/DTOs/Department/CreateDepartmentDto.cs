using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.Department;

/// <summary>
/// Payload for POST /api/departments.
/// </summary>
public class CreateDepartmentDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Maximum concurrent employees on leave. Defaults to 0 (unlimited) unless specified.</summary>
    public int OverlapLimit { get; set; } = 0;
}
