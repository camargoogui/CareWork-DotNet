namespace CareWork.Infrastructure.Models;

public class Checkin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Mood { get; set; } // 1-5
    public int Stress { get; set; } // 1-5
    public int Sleep { get; set; } // 1-5
    public string? Notes { get; set; } // Notas opcionais para contexto
    public List<string> Tags { get; set; } = new(); // Tags para categorização
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public User? User { get; set; }
}

