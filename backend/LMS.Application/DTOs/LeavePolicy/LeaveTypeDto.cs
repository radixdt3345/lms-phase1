namespace LMS.Application.DTOs.LeavePolicy;

public class LeaveTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AnnualDays { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool RequiresHrApproval { get; set; }
    public bool IsActive { get; set; }
}
