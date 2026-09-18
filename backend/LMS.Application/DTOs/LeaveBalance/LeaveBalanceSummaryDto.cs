namespace LMS.Application.DTOs.LeaveBalance;

/// <summary>
/// Aggregate summary of all leave balances for the current year (HRAdmin view).
/// </summary>
public class LeaveBalanceSummaryDto
{
    public int Year { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalDaysAllocated { get; set; }
    public decimal TotalDaysUsed { get; set; }
    public decimal TotalDaysPending { get; set; }
    public decimal TotalDaysAvailable { get; set; }
}
