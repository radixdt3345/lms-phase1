using LMS.Application.DTOs.Employee;

namespace LMS.Application.Interfaces;

/// <summary>
/// Employee profile CRUD operations — F-02 API layer.
/// </summary>
public interface IEmployeeService
{
    /// <summary>Returns a paged list of active (non-deleted) employee profiles.</summary>
    Task<PagedResult<EmployeeProfileDto>> GetAllAsync(int page, int pageSize, Guid? departmentId = null);

    /// <summary>Returns a single employee profile by ID, or null if not found / soft-deleted.</summary>
    Task<EmployeeProfileDto?> GetByIdAsync(Guid id);

    /// <summary>Returns an employee profile by UserId, or null if not found / soft-deleted.</summary>
    Task<EmployeeProfileDto?> GetByUserIdAsync(Guid userId);

    /// <summary>Creates a new employee profile. Throws InvalidOperationException if EmployeeCode is not unique.</summary>
    Task<EmployeeProfileDto> CreateAsync(CreateEmployeeProfileDto dto);

    /// <summary>Updates an existing employee profile. Throws KeyNotFoundException when not found.</summary>
    Task<EmployeeProfileDto> UpdateAsync(Guid id, UpdateEmployeeProfileDto dto);

    /// <summary>Soft-deletes an employee profile (sets DeletedAt). Returns true on success, false if not found.</summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>Returns leave balances for a given employee profile ID.</summary>
    Task<IEnumerable<EmployeeLeaveBalanceDto>> GetLeaveBalancesAsync(Guid employeeId);

    /// <summary>Returns documents for a given employee profile ID.</summary>
    Task<IEnumerable<EmployeeDocumentDto>> GetDocumentsAsync(Guid employeeId);
}
