using System;

namespace LMS.Domain.Entities;

public class EmployeeLeaveBalance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal Used { get; set; }
    public decimal Carried { get; set; }
    public decimal Available { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
