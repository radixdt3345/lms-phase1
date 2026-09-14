namespace LMS.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string AzureAdObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? ManagerAzureAdObjectId { get; set; }
    public bool IsActive { get; set; } = true;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public enum UserStatus
{
    Active,
    Locked,
    Inactive
}
