namespace LMS.Domain.Entities;

public class DashboardCache
{
    public Guid Id { get; set; }
    public string DashboardType { get; set; } = string.Empty; // "TeamLeave", "Approvals", "CompOff", "Overview"
    public string? FilterKey { get; set; } // e.g., department ID or manager ID
    public string CacheData { get; set; } = string.Empty; // JSON payload
    public DateTime ComputedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; } // soft delete
}
