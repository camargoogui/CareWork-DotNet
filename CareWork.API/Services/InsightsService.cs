using Microsoft.EntityFrameworkCore;
using CareWork.Infrastructure.Models;
using CareWork.API.Models.DTOs;
using CareWork.Infrastructure.Data;

namespace CareWork.API.Services;

public class InsightsService : IInsightsService
{
    private readonly CareWorkDbContext _context;
    private readonly ITipService _tipService;

    public InsightsService(CareWorkDbContext context, ITipService tipService)
    {
        _context = context;
        _tipService = tipService;
    }

    public async Task<TrendsInsightDto> GetTrendsAsync(Guid userId, string period)
    {
        var (startDate, endDate) = GetPeriodDates(period);
        
        var checkins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt.Date >= startDate.Date && 
                       c.CreatedAt.Date <= endDate.Date)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        if (!checkins.Any())
        {
            return new TrendsInsightDto
            {
                UserId = userId,
                Period = period,
                StartDate = startDate,
                EndDate = endDate,
                Insights = new List<string> { "Não há dados suficientes para análise" }
            };
        }

        var moodTrend = AnalyzeTrend(checkins.Select(c => (double)c.Mood).ToList());
        var stressTrend = AnalyzeTrend(checkins.Select(c => (double)c.Stress).ToList());
        var sleepTrend = AnalyzeTrend(checkins.Select(c => (double)c.Sleep).ToList());

        var insights = GenerateInsights(moodTrend, stressTrend, sleepTrend, checkins);
        var alerts = GenerateAlerts(moodTrend, stressTrend, sleepTrend);

        return new TrendsInsightDto
        {
            UserId = userId,
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            Mood = moodTrend,
            Stress = stressTrend,
            Sleep = sleepTrend,
            Insights = insights,
            Alerts = alerts
        };
    }

    public async Task<StreakDto> GetStreakAsync(Guid userId)
    {
        var checkins = await _context.Checkins
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        if (!checkins.Any())
        {
            return new StreakDto
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                IsActive = false
            };
        }

        var currentStreak = CalculateCurrentStreak(checkins);
        var longestStreak = CalculateLongestStreak(checkins);
        var lastCheckin = checkins.First().CreatedAt.Date;
        var today = DateTime.UtcNow.Date;
        var isActive = lastCheckin == today;

        return new StreakDto
        {
            UserId = userId,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            LastCheckinDate = lastCheckin,
            IsActive = isActive
        };
    }

    public async Task<ComparisonDto> ComparePeriodsAsync(Guid userId, DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        var period1Checkins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt >= start1 && 
                       c.CreatedAt <= end1)
            .ToListAsync();

        var period2Checkins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt >= start2 && 
                       c.CreatedAt <= end2)
            .ToListAsync();

        var period1 = new PeriodComparisonDto
        {
            StartDate = start1,
            EndDate = end1,
            Averages = CalculateAverages(period1Checkins),
            TotalCheckins = period1Checkins.Count
        };

        var period2 = new PeriodComparisonDto
        {
            StartDate = start2,
            EndDate = end2,
            Averages = CalculateAverages(period2Checkins),
            TotalCheckins = period2Checkins.Count
        };

        var comparison = new ComparisonMetricsDto
        {
            MoodChange = CalculatePercentageChange(period1.Averages.Mood, period2.Averages.Mood),
            StressChange = CalculatePercentageChange(period1.Averages.Stress, period2.Averages.Stress),
            SleepChange = CalculatePercentageChange(period1.Averages.Sleep, period2.Averages.Sleep),
            OverallTrend = DetermineOverallTrend(period1.Averages, period2.Averages),
            Summary = GenerateComparisonSummary(period1, period2)
        };

        return new ComparisonDto
        {
            UserId = userId,
            Period1 = period1,
            Period2 = period2,
            Comparison = comparison
        };
    }

    public async Task<List<TipDto>> GetRecommendedTipsAsync(Guid userId)
    {
        // Buscar check-ins dos últimos 7 dias (mesmo período do trends)
        var (startDate, endDate) = GetPeriodDates("week");
        
        var checkins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt.Date >= startDate.Date && 
                       c.CreatedAt.Date <= endDate.Date)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        if (!checkins.Any())
        {
            // Se não há check-ins recentes, buscar todos os check-ins
            var allCheckins = await _context.Checkins
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(7)
                .ToListAsync();

            if (!allCheckins.Any())
            {
                // Se não há check-ins, retornar dicas gerais de Wellness
                var generalTips = await _tipService.GetTipsAsync(1, 10, "Wellness");
                return generalTips.Data.Take(5).ToList();
            }

            checkins = allCheckins.OrderBy(c => c.CreatedAt).ToList();
        }

        // Calcular tendências (mesma lógica do GetTrendsAsync)
        var moodTrend = AnalyzeTrend(checkins.Select(c => (double)c.Mood).ToList());
        var stressTrend = AnalyzeTrend(checkins.Select(c => (double)c.Stress).ToList());
        var sleepTrend = AnalyzeTrend(checkins.Select(c => (double)c.Sleep).ToList());

        var recommendedCategories = new List<string>();

        // Lógica de recomendação baseada em MÉDIAS e TENDÊNCIAS (consistente com insights)
        // Prioridade: Identificar áreas que PRECISAM de atenção (problemas reais)
        
        // 1. Sleep: Recomendar se está ruim OU piorando
        // Sleep ruim = valores baixos (≤ 3.0) ou piorando (trend "declining")
        // NÃO recomendar se está melhorando (trend "improving")
        if (sleepTrend.Trend == "declining" && sleepTrend.Average <= 3.5)
        {
            // Sleep piorando - prioridade ALTA (precisa de atenção urgente)
            recommendedCategories.Add("Sleep");
        }
        else if (sleepTrend.Average <= 3.0)
        {
            // Sleep já está ruim mesmo que não esteja piorando
            recommendedCategories.Add("Sleep");
        }
        // Se sleep está melhorando (trend "improving") ou estável em bom nível, NÃO recomendar
        
        // 2. Mood: Recomendar se está ruim OU piorando
        // Mood ruim = valores baixos (≤ 3.0) ou piorando (trend "declining")
        // NÃO recomendar se está melhorando (trend "improving")
        if (moodTrend.Trend == "declining" && moodTrend.Average <= 3.5)
        {
            // Mood piorando - prioridade ALTA (precisa de atenção urgente)
            recommendedCategories.Add("Mood");
        }
        else if (moodTrend.Average <= 3.0)
        {
            // Mood já está ruim mesmo que não esteja piorando
            recommendedCategories.Add("Mood");
        }
        // Se mood está melhorando (trend "improving") ou estável em bom nível, NÃO recomendar

        // 3. Stress: Recomendar APENAS se está alto OU aumentando
        // Para stress, valores altos são ruins
        // "improving" trend = valores aumentando = piorando para stress
        // "declining" trend = valores diminuindo = melhorando para stress (NÃO recomendar)
        if (stressTrend.Average >= 3.5)
        {
            // Stress alto - sempre recomendar (independente da tendência)
            recommendedCategories.Add("Stress");
        }
        else if (stressTrend.Trend == "improving" && stressTrend.Average >= 3.0)
        {
            // Stress aumentando e já em nível preocupante (≥ 3.0)
            recommendedCategories.Add("Stress");
        }
        // NÃO recomendar Stress se:
        // - Média < 3.5 E trend não é "improving" (está baixo ou diminuindo)
        // - Média < 3.0 (mesmo que trend seja "improving", ainda não é preocupante)

        // 4. Wellness: Recomendar se tudo está bem (manter progresso)
        // Apenas se NÃO há problemas identificados acima
        if (!recommendedCategories.Any() &&
            moodTrend.Average >= 3.5 &&
            stressTrend.Average < 3.5 &&
            sleepTrend.Average >= 3.5 &&
            moodTrend.Trend != "declining" &&
            stressTrend.Trend != "improving" &&
            sleepTrend.Trend != "declining")
        {
            // Tudo está bem - recomendar Wellness para manter o progresso
            recommendedCategories.Add("Wellness");
        }

        // 5. Fallback: Se não identificou nenhuma área problemática, usar Wellness
        if (!recommendedCategories.Any())
        {
            recommendedCategories.Add("Wellness");
        }

        // Buscar dicas por categoria
        var allTipsResult = await _tipService.GetTipsAsync(1, 50);
        var recommended = new List<TipDto>();

        // Priorizar categorias identificadas
        // Se há apenas 1 categoria problemática: até 5 tips dessa categoria
        // Se há múltiplas categorias: distribuir proporcionalmente
        var categoriesCount = recommendedCategories.Distinct().Count();
        var tipsPerCategory = categoriesCount == 1 ? 5 : (categoriesCount == 2 ? 3 : 2);

        foreach (var category in recommendedCategories.Distinct())
        {
            var categoryTips = allTipsResult.Data
                .Where(t => (t.Category ?? "Wellness") == category)
                .Where(t => !recommended.Any(r => r.Id == t.Id))
                .Take(tipsPerCategory)
                .ToList();
            
            recommended.AddRange(categoryTips);
        }

        // Se não encontrou suficientes das categorias identificadas, adicionar apenas Wellness
        // NÃO adicionar outras categorias que não foram identificadas como problemáticas
        if (recommended.Count < 5)
        {
            var wellnessTips = allTipsResult.Data
                .Where(t => (t.Category ?? "Wellness") == "Wellness")
                .Where(t => !recommended.Any(r => r.Id == t.Id))
                .Take(5 - recommended.Count)
                .ToList();
            recommended.AddRange(wellnessTips);
        }

        return recommended.Take(5).ToList();
    }

    // Métodos auxiliares privados
    private (DateTime start, DateTime end) GetPeriodDates(string period)
    {
        // Usar o final do dia atual para incluir todos os check-ins de hoje
        var end = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // 23:59:59.9999999 do dia atual
        DateTime start;

        switch (period.ToLower())
        {
            case "week":
                start = DateTime.UtcNow.Date.AddDays(-7);
                break;
            case "month":
                start = DateTime.UtcNow.Date.AddMonths(-1);
                break;
            case "year":
                start = DateTime.UtcNow.Date.AddYears(-1);
                break;
            default:
                start = DateTime.UtcNow.Date.AddDays(-7);
                break;
        }

        return (start, end);
    }

    private TrendAnalysisDto AnalyzeTrend(List<double> values)
    {
        if (!values.Any())
            return new TrendAnalysisDto();

        var average = values.Average();
        var firstHalf = values.Take(values.Count / 2).Average();
        var secondHalf = values.Skip(values.Count / 2).Average();
        
        var change = secondHalf - firstHalf;
        var changePercentage = firstHalf != 0 ? (change / firstHalf) * 100 : 0;

        string trend;
        if (changePercentage > 5)
            trend = "improving";
        else if (changePercentage < -5)
            trend = "declining";
        else
            trend = "stable";

        return new TrendAnalysisDto
        {
            Average = Math.Round(average, 2),
            Trend = trend,
            ChangePercentage = Math.Round(changePercentage, 2)
        };
    }

    private int CalculateCurrentStreak(List<Checkin> checkins)
    {
        if (!checkins.Any()) return 0;

        var dates = checkins.Select(c => c.CreatedAt.Date).Distinct().OrderByDescending(d => d).ToList();
        if (!dates.Any()) return 0;

        var streak = 1;
        var currentDate = dates.First();

        for (int i = 1; i < dates.Count; i++)
        {
            var expectedDate = currentDate.AddDays(-1);
            if (dates[i] == expectedDate)
            {
                streak++;
                currentDate = dates[i];
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    private int CalculateLongestStreak(List<Checkin> checkins)
    {
        var dates = checkins.Select(c => c.CreatedAt.Date).Distinct().OrderByDescending(d => d).ToList();
        if (!dates.Any()) return 0;

        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            var daysDiff = (dates[i - 1] - dates[i]).Days;
            if (daysDiff == 1)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }

    private AveragesDto CalculateAverages(List<Checkin> checkins)
    {
        if (!checkins.Any())
            return new AveragesDto();

        return new AveragesDto
        {
            Mood = checkins.Average(c => c.Mood),
            Stress = checkins.Average(c => c.Stress),
            Sleep = checkins.Average(c => c.Sleep)
        };
    }

    private double CalculatePercentageChange(double oldValue, double newValue)
    {
        if (oldValue == 0) return 0;
        return Math.Round(((newValue - oldValue) / oldValue) * 100, 2);
    }

    private string DetermineOverallTrend(AveragesDto period1, AveragesDto period2)
    {
        var moodBetter = period2.Mood > period1.Mood;
        var stressBetter = period2.Stress < period1.Stress;
        var sleepBetter = period2.Sleep > period1.Sleep;

        var improvements = (moodBetter ? 1 : 0) + (stressBetter ? 1 : 0) + (sleepBetter ? 1 : 0);

        if (improvements >= 2)
            return "better";
        else if (improvements == 0)
            return "worse";
        else
            return "similar";
    }

    private string GenerateComparisonSummary(PeriodComparisonDto period1, PeriodComparisonDto period2)
    {
        var moodChange = period2.Averages.Mood - period1.Averages.Mood;
        var stressChange = period2.Averages.Stress - period1.Averages.Stress;
        var sleepChange = period2.Averages.Sleep - period1.Averages.Sleep;

        var parts = new List<string>();

        if (Math.Abs(moodChange) > 0.3)
            parts.Add($"Humor {(moodChange > 0 ? "melhorou" : "piorou")} {Math.Abs(moodChange):F1} pontos");

        if (Math.Abs(stressChange) > 0.3)
            parts.Add($"Stress {(stressChange < 0 ? "diminuiu" : "aumentou")} {Math.Abs(stressChange):F1} pontos");

        if (Math.Abs(sleepChange) > 0.3)
            parts.Add($"Sono {(sleepChange > 0 ? "melhorou" : "piorou")} {Math.Abs(sleepChange):F1} pontos");

        return parts.Any() ? string.Join(". ", parts) + "." : "Mudanças mínimas entre os períodos.";
    }

    private List<string> GenerateInsights(TrendAnalysisDto mood, TrendAnalysisDto stress, TrendAnalysisDto sleep, List<Checkin> checkins)
    {
        var insights = new List<string>();

        if (mood.Trend == "improving")
            insights.Add("Seu humor está melhorando! Continue assim.");
        else if (mood.Trend == "declining")
            insights.Add("Seu humor está em declínio. Considere buscar apoio.");

        if (stress.Trend == "declining")
            insights.Add("Ótimo! Seu nível de stress está diminuindo.");
        else if (stress.Trend == "improving" && stress.Average > 3)
            insights.Add("Seu stress está aumentando. Tente técnicas de relaxamento.");

        if (sleep.Trend == "improving")
            insights.Add("Sua qualidade de sono está melhorando!");
        else if (sleep.Trend == "declining")
            insights.Add("Sua qualidade de sono precisa de atenção.");

        // Análise de correlação
        if (checkins.Count >= 7)
        {
            var highStressDays = checkins.Where(c => c.Stress >= 4).Count();
            var lowSleepDays = checkins.Where(c => c.Sleep <= 2).Count();
            
            if (highStressDays > checkins.Count * 0.5)
                insights.Add("Você está tendo muitos dias estressantes. Considere estratégias de gerenciamento de stress.");
        }

        return insights;
    }

    private List<AlertDto> GenerateAlerts(TrendAnalysisDto mood, TrendAnalysisDto stress, TrendAnalysisDto sleep)
    {
        var alerts = new List<AlertDto>();

        if (mood.Average <= 2)
        {
            alerts.Add(new AlertDto
            {
                Type = "warning",
                Message = "Seu humor está muito baixo. Considere buscar apoio profissional.",
                Category = "mood"
            });
        }

        if (stress.Average >= 4)
        {
            alerts.Add(new AlertDto
            {
                Type = "warning",
                Message = "Seu nível de stress está alto. Pratique técnicas de relaxamento.",
                Category = "stress"
            });
        }

        if (sleep.Average <= 2)
        {
            alerts.Add(new AlertDto
            {
                Type = "warning",
                Message = "Sua qualidade de sono precisa de atenção.",
                Category = "sleep"
            });
        }

        if (mood.Average >= 4 && stress.Average <= 2 && sleep.Average >= 4)
        {
            alerts.Add(new AlertDto
            {
                Type = "success",
                Message = "Parabéns! Você está mantendo um excelente bem-estar!",
                Category = "overall"
            });
        }

        return alerts;
    }
}

