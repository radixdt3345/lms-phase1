namespace LMS.Domain.Entities;

public class ReportJob
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty; // "LeaveSummary", "CompOffSummary", "ApprovalHistory"
    public string RequestedByUserId { get; set; } = string.Empty;
    public string? FilterJson { get; set; } // JSON filter params (date range, department, etc.)
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public string? OutputPath { get; set; } // blob storage path when complete
    public string? ErrorMessage { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // soft delete
}
