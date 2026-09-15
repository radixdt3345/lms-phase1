using System;

namespace LMS.Domain.Entities;

public class EmployeeProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public DateOnly JoiningDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string EmploymentType { get; set; } = "Full-Time";
    public Guid? ManagerUserId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public User? Manager { get; set; }
    public Department? Department { get; set; }
}
