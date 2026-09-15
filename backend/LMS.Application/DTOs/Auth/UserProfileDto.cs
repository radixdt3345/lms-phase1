namespace LMS.Application.DTOs.Auth;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string AzureAdObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
}
