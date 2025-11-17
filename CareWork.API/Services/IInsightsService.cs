using CareWork.API.Models.DTOs;

namespace CareWork.API.Services;

public interface IInsightsService
{
    Task<TrendsInsightDto> GetTrendsAsync(Guid userId, string period);
    Task<StreakDto> GetStreakAsync(Guid userId);
    Task<ComparisonDto> ComparePeriodsAsync(Guid userId, DateTime start1, DateTime end1, DateTime start2, DateTime end2);
    Task<List<TipDto>> GetRecommendedTipsAsync(Guid userId);
}

