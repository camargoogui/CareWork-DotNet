namespace CareWork.API.Models.DTOs;

public class TrendsInsightDto
{
    public Guid UserId { get; set; }
    public string Period { get; set; } = string.Empty; // "week", "month", "year"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TrendAnalysisDto Mood { get; set; } = new();
    public TrendAnalysisDto Stress { get; set; } = new();
    public TrendAnalysisDto Sleep { get; set; } = new();
    public List<string> Insights { get; set; } = new(); // Insights em texto
    public List<AlertDto> Alerts { get; set; } = new(); // Alertas importantes
}

public class TrendAnalysisDto
{
    public double Average { get; set; }
    public string Trend { get; set; } = string.Empty; // "improving", "declining", "stable"
    public double ChangePercentage { get; set; } // % de mudança
    public int? BestDay { get; set; } // Dia da semana (0=Sunday, 6=Saturday)
    public int? WorstDay { get; set; }
}

public class AlertDto
{
    public string Type { get; set; } = string.Empty; // "warning", "info", "success"
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "mood", "stress", "sleep"
}

public class StreakDto
{
    public Guid UserId { get; set; }
    public int CurrentStreak { get; set; } // Dias consecutivos
    public int LongestStreak { get; set; } // Maior sequência já alcançada
    public DateTime? LastCheckinDate { get; set; }
    public bool IsActive { get; set; } // Se o streak está ativo hoje
}

public class ComparisonDto
{
    public Guid UserId { get; set; }
    public PeriodComparisonDto Period1 { get; set; } = new();
    public PeriodComparisonDto Period2 { get; set; } = new();
    public ComparisonMetricsDto Comparison { get; set; } = new();
}

public class PeriodComparisonDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AveragesDto Averages { get; set; } = new();
    public int TotalCheckins { get; set; }
}

public class ComparisonMetricsDto
{
    public double MoodChange { get; set; } // % de mudança
    public double StressChange { get; set; }
    public double SleepChange { get; set; }
    public string OverallTrend { get; set; } = string.Empty; // "better", "worse", "similar"
    public string Summary { get; set; } = string.Empty; // Resumo em texto
}

