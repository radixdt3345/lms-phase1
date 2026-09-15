namespace LMS.Application.DTOs.Employee;

/// <summary>
/// Read model returned by employee profile endpoints (F-02 API layer).
/// </summary>
public class EmployeeProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public DateOnly JoiningDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string EmploymentType { get; set; } = "Full-Time";
    public Guid? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
