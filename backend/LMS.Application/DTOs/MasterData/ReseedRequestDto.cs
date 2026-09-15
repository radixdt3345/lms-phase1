using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs.MasterData;

public class ReseedRequestDto
{
    [Required]
    public string Section { get; set; } = "all";
}
