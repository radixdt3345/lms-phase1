namespace LMS.Domain.Entities;

/// <summary>
/// Well-known leave type name and code constants — single source of truth used
/// in seeding, business rules, and tests.
/// </summary>
public static class LeaveTypeNames
{
    // Display names
    public const string CasualLeave = "Casual Leave";
    public const string SickLeave = "Sick Leave";
    public const string EarnedLeave = "Earned Leave";
    public const string CompOff = "Comp-off";
    public const string UnpaidLeave = "Unpaid Leave";

    // Short codes
    public const string CasualLeaveCode = "CL";
    public const string SickLeaveCode = "SL";
    public const string EarnedLeaveCode = "EL";
    public const string CompOffCode = "CO";
    public const string UnpaidLeaveCode = "UL";
}
