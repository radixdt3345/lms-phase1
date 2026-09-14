namespace LMS.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public static class RoleNames
{
    public const string HRAdmin = "HRAdmin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string Director = "Director";
    public const string SuperAdmin = "SuperAdmin";
}
