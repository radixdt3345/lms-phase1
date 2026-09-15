using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.Employee;

/// <summary>
/// Payload for POST /api/employees — create a new employee profile.
/// </summary>
public class CreateEmployeeProfileDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [Required]
    public DateOnly JoiningDate { get; set; }

    public DateOnly? TerminationDate { get; set; }

    [MaxLength(50)]
    public string EmploymentType { get; set; } = "Full-Time";

    public Guid? ManagerUserId { get; set; }

    public Guid? DepartmentId { get; set; }
}
