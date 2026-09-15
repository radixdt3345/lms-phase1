namespace LMS.Domain.Entities;

/// <summary>
/// Represents a public holiday entry (FR-76, FR-77).
/// Used by sandwich-rule calculation, working-day counting, team overlap checks,
/// comp-off eligibility validation, and calendar date picker greying.
/// </summary>
public class PublicHoliday
{
    public Guid Id { get; set; }

    /// <summary>Display name of the holiday, e.g. "Republic Day".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Calendar date of the holiday.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Calendar year — denormalised for efficient year-range queries.</summary>
    public int Year { get; set; }

    /// <summary>Optional description or note.</summary>
    public string? Description { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "IN". Null means company-wide.</summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// When true the holiday is optional — employees may choose it from a floating holiday pool.
    /// When false it is a mandatory gazetted holiday.
    /// </summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>Inactive holidays are excluded from all business-day calculations.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
