using LMS.Application.DTOs.Holiday;

namespace LMS.Application.Interfaces;

/// <summary>
/// Service contract for public holiday management (F-10).
/// </summary>
public interface IPublicHolidayService
{
    /// <summary>Returns all holidays for the given calendar year.</summary>
    Task<IEnumerable<PublicHolidayDto>> GetByYearAsync(int year);

    /// <summary>Returns all holidays whose Date falls within [from, to] inclusive.</summary>
    Task<IEnumerable<PublicHolidayDto>> GetByDateRangeAsync(DateOnly from, DateOnly to);

    /// <summary>Returns the holiday with the given ID, or null if not found.</summary>
    Task<PublicHolidayDto?> GetByIdAsync(Guid id);

    /// <summary>Returns true when the given date is an active public holiday.</summary>
    Task<bool> IsHolidayAsync(DateOnly date);

    /// <summary>
    /// Counts working days in [from, to] inclusive by subtracting
    /// Saturdays, Sundays, and active public holidays.
    /// </summary>
    Task<int> GetWorkingDaysAsync(DateOnly from, DateOnly to);

    /// <summary>Creates a new holiday record.</summary>
    Task<PublicHolidayDto> CreateAsync(CreatePublicHolidayDto dto);

    /// <summary>Applies a partial update to an existing holiday.</summary>
    Task<PublicHolidayDto> UpdateAsync(Guid id, UpdatePublicHolidayDto dto);

    /// <summary>Hard-deletes a holiday record. Returns false if not found.</summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Bulk-inserts holidays, skipping any entry whose Name+Date pair already exists.
    /// Returns the newly created records only.
    /// </summary>
    Task<IEnumerable<PublicHolidayDto>> BulkImportAsync(IEnumerable<CreatePublicHolidayDto> dtos);
}
