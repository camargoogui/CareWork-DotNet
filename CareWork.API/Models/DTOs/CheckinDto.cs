namespace CareWork.API.Models.DTOs;

public class CheckinDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Mood { get; set; }
    public int Stress { get; set; }
    public int Sleep { get; set; }
    public string? Notes { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

