using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.Department;

/// <summary>
/// Payload for PUT /api/departments/{id}. All fields are optional; only supplied fields are updated.
/// </summary>
public class UpdateDepartmentDto
{
    public string? Name { get; set; }

    [MaxLength(10)]
    public string? Code { get; set; }

    public int? OverlapLimit { get; set; }

    public bool? IsActive { get; set; }
}
