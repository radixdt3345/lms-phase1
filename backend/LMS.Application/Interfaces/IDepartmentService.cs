using LMS.Application.DTOs.Department;

namespace LMS.Application.Interfaces;

/// <summary>
/// Department CRUD operations — F-03 API layer (FR-19 to FR-26).
/// </summary>
public interface IDepartmentService
{
    /// <summary>Returns all non-deleted departments ordered by Name.</summary>
    Task<IEnumerable<DepartmentDto>> GetAllAsync();

    /// <summary>Returns a single department, or null if not found / soft-deleted.</summary>
    Task<DepartmentDto?> GetByIdAsync(Guid id);

    /// <summary>Creates a new department. Throws InvalidOperationException if Code is not unique.</summary>
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);

    /// <summary>Updates an existing department. Throws KeyNotFoundException when not found; InvalidOperationException on Code conflict.</summary>
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto dto);

    /// <summary>Soft-deletes a department (sets DeletedAt). Returns true on success, false if not found.</summary>
    Task<bool> SoftDeleteAsync(Guid id);

    /// <summary>Returns the count of currently active, non-deleted departments.</summary>
    Task<int> GetActiveCountAsync();
}
