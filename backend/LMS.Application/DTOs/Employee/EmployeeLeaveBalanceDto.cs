namespace LMS.Application.DTOs.Employee;

/// <summary>
/// Read model for an employee leave balance entry.
/// </summary>
public class EmployeeLeaveBalanceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string? LeaveTypeName { get; set; }
    public int Year { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal Used { get; set; }
    public decimal Carried { get; set; }
    public decimal Available { get; set; }
}
