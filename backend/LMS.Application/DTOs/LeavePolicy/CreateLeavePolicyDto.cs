using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.LeavePolicy;

public class CreateLeavePolicyDto
{
    [Required]
    public Guid LeaveTypeId { get; set; }

    public string? Name { get; set; }
    public int AnnualAllotment { get; set; }
    public int MaxCarryForward { get; set; }
    public int MaxConsecutiveDays { get; set; } = 30;
    public int MinNoticeDays { get; set; }
    public bool AccrualMonthly { get; set; }
    public decimal? AccrualRate { get; set; }
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}
