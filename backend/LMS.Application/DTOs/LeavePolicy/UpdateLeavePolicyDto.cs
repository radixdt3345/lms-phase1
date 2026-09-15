namespace LMS.Application.DTOs.LeavePolicy;

public class UpdateLeavePolicyDto
{
    public string? Name { get; set; }
    public int? AnnualAllotment { get; set; }
    public int? MaxCarryForward { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public int? MinNoticeDays { get; set; }
    public bool? AccrualMonthly { get; set; }
    public decimal? AccrualRate { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
