namespace LMS.Application.DTOs.LeavePolicy;

public class UpdateLeaveTypeDto
{
    public string? Name { get; set; }
    public int? AnnualDays { get; set; }
    public bool? RequiresAttachment { get; set; }
    public bool? RequiresHrApproval { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}
