using System.ComponentModel.DataAnnotations;

namespace CareWork.API.Models.DTOs;

public class UpdateTipDto
{
    [MaxLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    public string? Title { get; set; }

    [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }
}

