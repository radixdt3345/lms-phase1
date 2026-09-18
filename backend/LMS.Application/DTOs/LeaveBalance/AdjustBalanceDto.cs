using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.LeaveBalance;

/// <summary>
/// Payload for HR Admin balance adjustment (positive or negative).
/// </summary>
public class AdjustBalanceDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100.")]
    public int Year { get; set; }

    /// <summary>
    /// The amount to add (positive) or deduct (negative) from AdjustedDays.
    /// </summary>
    [Required]
    public decimal Adjustment { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
