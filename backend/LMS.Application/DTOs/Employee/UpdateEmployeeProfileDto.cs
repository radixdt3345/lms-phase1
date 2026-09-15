using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.Employee;

/// <summary>
/// Payload for PUT /api/employees/{id} — update an existing employee profile.
/// All fields are optional; only non-null fields are applied.
/// </summary>
public class UpdateEmployeeProfileDto
{
    [MaxLength(50)]
    public string? EmployeeCode { get; set; }

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    public DateOnly? JoiningDate { get; set; }

    public DateOnly? TerminationDate { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public Guid? ManagerUserId { get; set; }

    public Guid? DepartmentId { get; set; }
}
