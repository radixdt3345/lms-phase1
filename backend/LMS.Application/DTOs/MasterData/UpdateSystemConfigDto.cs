using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.MasterData;

public class UpdateSystemConfigDto
{
    [Required]
    public string Value { get; set; } = string.Empty;
}
