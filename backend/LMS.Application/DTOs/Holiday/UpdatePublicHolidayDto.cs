namespace LMS.Application.DTOs.Holiday;

/// <summary>
/// DTO for partial updates to a public holiday. All fields are optional.
/// </summary>
public class UpdatePublicHolidayDto
{
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public string? Description { get; set; }
    public bool? IsOptional { get; set; }
    public bool? IsActive { get; set; }
}
