using LMS.Application.DTOs.LeaveBalance;

namespace LMS.Application.Interfaces;

/// <summary>
/// Leave Balance management operations — F-05 API layer (FR-33 to FR-41).
/// Balances are always fetched fresh from the database (FR-41 — no caching).
/// </summary>
public interface ILeaveBalanceService
{
    /// <summary>Returns all leave balances across all employees (HRAdmin only).</summary>
    Task<IEnumerable<LeaveBalanceDto>> GetAllAsync();

    /// <summary>Returns leave balances for the given employee in the current year.</summary>
    Task<IEnumerable<LeaveBalanceDto>> GetByEmployeeIdAsync(Guid employeeId);

    /// <summary>
    /// Applies a manual adjustment (positive or negative) to the employee's AdjustedDays
    /// for a specific leave type and year (HRAdmin only).
    /// Throws <see cref="KeyNotFoundException"/> when the balance record does not exist.
    /// </summary>
    Task<LeaveBalanceDto> AdjustBalanceAsync(AdjustBalanceDto dto);

    /// <summary>Returns aggregate summary stats for the current calendar year (HRAdmin only).</summary>
    Task<LeaveBalanceSummaryDto> GetSummaryAsync();
}
