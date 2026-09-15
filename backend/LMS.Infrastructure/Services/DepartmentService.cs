using LMS.Application.DTOs.Department;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements department CRUD with soft-delete semantics (FR-23).
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(LmsDbContext context, ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        var departments = await _context.Departments
            .Where(d => d.DeletedAt == null)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return departments.Select(MapToDto);
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

        return department is null ? null : MapToDto(department);
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _context.Departments
            .CountAsync(d => d.IsActive && d.DeletedAt == null);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code.ToLower() == dto.Code.ToLower() && d.DeletedAt == null);

        if (codeExists)
        {
            throw new InvalidOperationException($"A department with code '{dto.Code}' already exists.");
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code.ToUpper(),
            OverlapLimit = dto.OverlapLimit,
            IsActive = true
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created department {DepartmentId} with code {Code}", department.Id, department.Code);
        return MapToDto(department);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

        if (department is null)
        {
            throw new KeyNotFoundException($"Department with ID '{id}' was not found.");
        }

        // Check Code uniqueness only when Code is being changed
        if (dto.Code is not null &&
            !string.Equals(dto.Code, department.Code, StringComparison.OrdinalIgnoreCase))
        {
            var codeExists = await _context.Departments
                .AnyAsync(d => d.Code.ToLower() == dto.Code.ToLower()
                            && d.Id != id
                            && d.DeletedAt == null);

            if (codeExists)
            {
                throw new InvalidOperationException($"A department with code '{dto.Code}' already exists.");
            }

            department.Code = dto.Code.ToUpper();
        }

        if (dto.Name is not null)
            department.Name = dto.Name;

        if (dto.OverlapLimit.HasValue)
            department.OverlapLimit = dto.OverlapLimit.Value;

        if (dto.IsActive.HasValue)
            department.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated department {DepartmentId}", department.Id);
        return MapToDto(department);
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

        if (department is null)
        {
            return false;
        }

        department.DeletedAt = DateTime.UtcNow;
        department.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted department {DepartmentId}", department.Id);
        return true;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static DepartmentDto MapToDto(Department d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Code = d.Code,
        OverlapLimit = d.OverlapLimit,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };
}
