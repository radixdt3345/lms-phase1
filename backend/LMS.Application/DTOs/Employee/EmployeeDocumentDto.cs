namespace LMS.Application.DTOs.Employee;

/// <summary>
/// Read model for an employee document record.
/// </summary>
public class EmployeeDocumentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public Guid UploadedByUserId { get; set; }
}
