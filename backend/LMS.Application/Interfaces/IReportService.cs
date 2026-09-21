namespace LMS.Application.Interfaces;

/// <summary>
/// Report service interface — F-12 (generate and download CSV reports).
/// </summary>
public interface IReportService
{
    Task<ReportJobDto> RequestReportAsync(string reportType, string userId, string? filterJson = null);
    Task<IEnumerable<ReportJobDto>> GetUserReportJobsAsync(string userId);
    Task<ReportJobDto?> GetReportJobByIdAsync(Guid id);
    Task<byte[]> DownloadReportAsync(Guid id); // returns CSV bytes
}
