using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.Holiday;

/// <summary>
/// DTO for creating a new public holiday.
/// </summary>
public class CreatePublicHolidayDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly Date { get; set; }

    public string? Description { get; set; }

    [MaxLength(2)]
    public string CountryCode { get; set; } = "IN";

    public bool IsOptional { get; set; } = false;
}
