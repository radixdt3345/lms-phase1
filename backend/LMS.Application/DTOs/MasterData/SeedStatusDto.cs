namespace LMS.Application.DTOs.MasterData;

public class SeedStatusDto
{
    public int TotalRoles { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalLeaveTypes { get; set; }
    public int TotalPublicHolidays { get; set; }
    public int TotalSystemConfigs { get; set; }
    public DateTimeOffset? LastSeededAt { get; set; }
    public bool IsHealthy { get; set; }
}
