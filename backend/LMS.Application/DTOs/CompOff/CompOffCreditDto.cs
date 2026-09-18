namespace LMS.Application.DTOs.CompOff;

/// <summary>
/// Read model for comp-off credit balance.
/// </summary>
public class CompOffCreditDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Sum of active (unexpired, unconsumed) comp-off credit days.</summary>
    public decimal ActiveDays { get; set; }

    /// <summary>Sum of used comp-off credit days in the current year.</summary>
    public decimal UsedDays { get; set; }

    /// <summary>Sum of expired comp-off credit days in the current year.</summary>
    public decimal ExpiredDays { get; set; }

    /// <summary>Individual credit records, ordered by expiry date ascending (soonest to expire first).</summary>
    public IEnumerable<CompOffCreditDetailDto> Credits { get; set; } = Enumerable.Empty<CompOffCreditDetailDto>();
}

/// <summary>
/// Detail for a single comp-off credit record.
/// </summary>
public class CompOffCreditDetailDto
{
    public Guid Id { get; set; }
    public DateOnly EarnedDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public decimal Days { get; set; }
    public string Status { get; set; } = string.Empty;
}
