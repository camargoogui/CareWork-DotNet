using CareWork.API.Models;
using CareWork.API.Models.DTOs;

namespace CareWork.API.Services;

public interface ICheckinService
{
    Task<PagedResponseDto<CheckinDto>> GetCheckinsAsync(Guid userId, int page, int pageSize);
    Task<CheckinDto?> GetCheckinByIdAsync(Guid id, Guid userId);
    Task<CheckinDto> CreateCheckinAsync(CreateCheckinDto dto, Guid userId);
    Task<CheckinDto?> UpdateCheckinAsync(Guid id, UpdateCheckinDto dto, Guid userId);
    Task<bool> DeleteCheckinAsync(Guid id, Guid userId);
    Task<WeeklyReportDto> GetWeeklyReportAsync(Guid userId, DateTime weekStart);
    Task<MonthlyReportDto> GetMonthlyReportAsync(Guid userId, int year, int month);
}

