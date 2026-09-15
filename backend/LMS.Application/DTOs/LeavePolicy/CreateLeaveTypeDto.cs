using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.LeavePolicy;

public class CreateLeaveTypeDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    public string Code { get; set; } = string.Empty;

    public int AnnualDays { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool RequiresHrApproval { get; set; }
    public string? Description { get; set; }
}
