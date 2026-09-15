namespace LMS.Domain.Entities;

/// <summary>
/// Stores system-wide configuration key/value pairs managed by SuperAdmin.
/// </summary>
public class SystemConfig
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEditable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
