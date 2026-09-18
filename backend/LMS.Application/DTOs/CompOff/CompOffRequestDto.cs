namespace LMS.Application.DTOs.CompOff;

/// <summary>
/// Read model returned by comp-off request endpoints.
/// </summary>
public class CompOffRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly DateWorked { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsHalfDay { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CalculatedCredit { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
