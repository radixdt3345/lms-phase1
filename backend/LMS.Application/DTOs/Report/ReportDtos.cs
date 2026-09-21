namespace LMS.Application.DTOs.Report;

/// <summary>
/// DTOs for F-12 Report &amp; CSV Export.
/// </summary>

public record ReportJobDto(
    Guid Id,
    string ReportType,
    string Status,
    string? OutputPath,
    DateTime RequestedAt,
    DateTime? CompletedAt);

public record CreateReportRequest(
    string ReportType,
    string? StartDate,
    string? EndDate,
    string? DepartmentId);
