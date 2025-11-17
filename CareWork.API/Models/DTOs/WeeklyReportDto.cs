namespace CareWork.API.Models.DTOs;

public class WeeklyReportDto
{
    public Guid UserId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public AveragesDto Averages { get; set; } = new();
    public List<DailyDataDto> DailyData { get; set; } = new();
}

public class AveragesDto
{
    public double Mood { get; set; }
    public double Stress { get; set; }
    public double Sleep { get; set; }
}

public class DailyDataDto
{
    public DateTime Date { get; set; }
    public int Mood { get; set; }
    public int Stress { get; set; }
    public int Sleep { get; set; }
}

