using System.ComponentModel.DataAnnotations;

namespace CareWork.API.Models.DTOs;

public class UpdateCheckinDto
{
    [Range(1, 5, ErrorMessage = "Mood must be between 1 and 5")]
    public int? Mood { get; set; }

    [Range(1, 5, ErrorMessage = "Stress must be between 1 and 5")]
    public int? Stress { get; set; }

    [Range(1, 5, ErrorMessage = "Sleep must be between 1 and 5")]
    public int? Sleep { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes must not exceed 1000 characters")]
    public string? Notes { get; set; }

    public List<string>? Tags { get; set; }
}

