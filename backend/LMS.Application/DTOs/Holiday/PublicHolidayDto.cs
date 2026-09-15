namespace LMS.Application.DTOs.Holiday;

/// <summary>
/// Read-only DTO for a public holiday record.
/// </summary>
public class PublicHolidayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Year { get; set; }
    public string? Description { get; set; }
    public string CountryCode { get; set; } = "IN";
    public bool IsOptional { get; set; }
    public bool IsActive { get; set; }
}
