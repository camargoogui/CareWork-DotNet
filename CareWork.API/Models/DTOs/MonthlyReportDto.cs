namespace CareWork.API.Models.DTOs;

public class MonthlyReportDto
{
    public Guid UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime MonthStart { get; set; }
    public DateTime MonthEnd { get; set; }
    public AveragesDto Averages { get; set; } = new();
    public AveragesDto? PreviousMonthAverages { get; set; }
    public List<WeeklySummaryDto> WeeklySummaries { get; set; } = new();
    public BestWorstDaysDto BestWorstDays { get; set; } = new();
    public int TotalCheckins { get; set; }
    public double CheckinFrequency { get; set; } // % de dias com check-in
}

public class WeeklySummaryDto
{
    public int WeekNumber { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public AveragesDto Averages { get; set; } = new();
    public int CheckinCount { get; set; }
}

public class BestWorstDaysDto
{
    public DaySummaryDto? BestDay { get; set; }
    public DaySummaryDto? WorstDay { get; set; }
}

public class DaySummaryDto
{
    public DateTime Date { get; set; }
    public int Mood { get; set; }
    public int Stress { get; set; }
    public int Sleep { get; set; }
    public double OverallScore { get; set; } // Score calculado
}

