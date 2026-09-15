using LMS.Application.DTOs.Holiday;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Services;

/// <summary>
/// EF Core implementation of IPublicHolidayService (F-10).
/// </summary>
public class PublicHolidayService : IPublicHolidayService
{
    private readonly LmsDbContext _context;

    public PublicHolidayService(LmsDbContext context)
    {
        _context = context;
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<PublicHolidayDto>> GetByYearAsync(int year)
    {
        var holidays = await _context.PublicHolidays
            .Where(h => h.Year == year)
            .OrderBy(h => h.Date)
            .ToListAsync();

        return holidays.Select(MapToDto);
    }

    public async Task<IEnumerable<PublicHolidayDto>> GetByDateRangeAsync(DateOnly from, DateOnly to)
    {
        var holidays = await _context.PublicHolidays
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .ToListAsync();

        return holidays.Select(MapToDto);
    }

    public async Task<PublicHolidayDto?> GetByIdAsync(Guid id)
    {
        var holiday = await _context.PublicHolidays.FindAsync(id);
        return holiday is null ? null : MapToDto(holiday);
    }

    public async Task<bool> IsHolidayAsync(DateOnly date)
    {
        return await _context.PublicHolidays
            .AnyAsync(h => h.Date == date && h.IsActive);
    }

    public async Task<int> GetWorkingDaysAsync(DateOnly from, DateOnly to)
    {
        if (from > to)
            return 0;

        // Collect active holiday dates in the range.
        var holidayDates = await _context.PublicHolidays
            .Where(h => h.IsActive && h.Date >= from && h.Date <= to)
            .Select(h => h.Date)
            .ToListAsync();

        var holidaySet = new HashSet<DateOnly>(holidayDates);

        int workingDays = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            // Skip weekends and holidays.
            if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                continue;
            if (holidaySet.Contains(d))
                continue;
            workingDays++;
        }

        return workingDays;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public async Task<PublicHolidayDto> CreateAsync(CreatePublicHolidayDto dto)
    {
        var holiday = new PublicHoliday
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Date = dto.Date,
            Year = dto.Date.Year,
            Description = dto.Description,
            CountryCode = dto.CountryCode,
            IsOptional = dto.IsOptional,
            IsActive = true
        };

        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync();

        return MapToDto(holiday);
    }

    public async Task<PublicHolidayDto> UpdateAsync(Guid id, UpdatePublicHolidayDto dto)
    {
        var holiday = await _context.PublicHolidays.FindAsync(id)
            ?? throw new KeyNotFoundException($"Public holiday {id} not found.");

        if (dto.Name is not null)
            holiday.Name = dto.Name;

        if (dto.Date.HasValue)
        {
            holiday.Date = dto.Date.Value;
            holiday.Year = dto.Date.Value.Year;
        }

        if (dto.Description is not null)
            holiday.Description = dto.Description;

        if (dto.IsOptional.HasValue)
            holiday.IsOptional = dto.IsOptional.Value;

        if (dto.IsActive.HasValue)
            holiday.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return MapToDto(holiday);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var holiday = await _context.PublicHolidays.FindAsync(id);
        if (holiday is null)
            return false;

        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PublicHolidayDto>> BulkImportAsync(IEnumerable<CreatePublicHolidayDto> dtos)
    {
        // Load existing Name+Date pairs to detect duplicates.
        var existing = await _context.PublicHolidays
            .Select(h => new { h.Name, h.Date })
            .ToListAsync();

        var existingSet = new HashSet<(string Name, DateOnly Date)>(
            existing.Select(e => (e.Name, e.Date)));

        var toInsert = new List<PublicHoliday>();

        foreach (var dto in dtos)
        {
            if (existingSet.Contains((dto.Name, dto.Date)))
                continue; // skip duplicate

            var holiday = new PublicHoliday
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Date = dto.Date,
                Year = dto.Date.Year,
                Description = dto.Description,
                CountryCode = dto.CountryCode,
                IsOptional = dto.IsOptional,
                IsActive = true
            };

            toInsert.Add(holiday);
            existingSet.Add((dto.Name, dto.Date)); // prevent intra-batch duplicates
        }

        if (toInsert.Count > 0)
        {
            _context.PublicHolidays.AddRange(toInsert);
            await _context.SaveChangesAsync();
        }

        return toInsert.Select(MapToDto);
    }

    // ── Mapping ────────────────────────────────────────────────────────────────

    private static PublicHolidayDto MapToDto(PublicHoliday h) => new()
    {
        Id = h.Id,
        Name = h.Name,
        Date = h.Date,
        Year = h.Year,
        Description = h.Description,
        CountryCode = h.CountryCode ?? "IN",
        IsOptional = h.IsOptional,
        IsActive = h.IsActive
    };
}
