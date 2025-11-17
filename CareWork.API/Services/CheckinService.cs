using Microsoft.EntityFrameworkCore;
using CareWork.Infrastructure.Models;
using CareWork.API.Models.DTOs;
using CareWork.Infrastructure.Data;
using AutoMapper;

namespace CareWork.API.Services;

public class CheckinService : ICheckinService
{
    private readonly CareWorkDbContext _context;
    private readonly IMapper _mapper;

    public CheckinService(CareWorkDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResponseDto<CheckinDto>> GetCheckinsAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Checkins
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var checkins = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Mapear manualmente para garantir consistência
        var checkinDtos = checkins.Select(c => new CheckinDto
        {
            Id = c.Id,
            UserId = c.UserId,
            Mood = c.Mood,
            Stress = c.Stress,
            Sleep = c.Sleep,
            Notes = c.Notes,
            Tags = c.Tags,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return new PagedResponseDto<CheckinDto>
        {
            Data = checkinDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
            Links = new LinksDto
            {
                Self = $"/api/v1/checkins?page={page}&pageSize={pageSize}",
                First = $"/api/v1/checkins?page=1&pageSize={pageSize}",
                Last = $"/api/v1/checkins?page={totalPages}&pageSize={pageSize}",
                Previous = page > 1 ? $"/api/v1/checkins?page={page - 1}&pageSize={pageSize}" : null,
                Next = page < totalPages ? $"/api/v1/checkins?page={page + 1}&pageSize={pageSize}" : null
            }
        };
    }

    public async Task<CheckinDto?> GetCheckinByIdAsync(Guid id, Guid userId)
    {
        var checkin = await _context.Checkins
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (checkin == null)
            return null;

        // Mapear manualmente para garantir consistência
        return new CheckinDto
        {
            Id = checkin.Id,
            UserId = checkin.UserId,
            Mood = checkin.Mood,
            Stress = checkin.Stress,
            Sleep = checkin.Sleep,
            Notes = checkin.Notes,
            Tags = checkin.Tags,
            CreatedAt = checkin.CreatedAt,
            UpdatedAt = checkin.UpdatedAt
        };
    }

    public async Task<CheckinDto> CreateCheckinAsync(CreateCheckinDto dto, Guid userId)
    {
        // Criar entidade com os valores do DTO
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Mood = dto.Mood,
            Stress = dto.Stress,
            Sleep = dto.Sleep,
            Notes = dto.Notes,
            Tags = dto.Tags ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        // Adicionar ao contexto
        _context.Checkins.Add(checkin);
        
        // Salvar no banco
        await _context.SaveChangesAsync();

        // Retornar DTO diretamente dos valores salvos (sem reload para evitar problemas)
        var checkinDto = new CheckinDto
        {
            Id = checkin.Id,
            UserId = checkin.UserId,
            Mood = checkin.Mood,
            Stress = checkin.Stress,
            Sleep = checkin.Sleep,
            Notes = checkin.Notes,
            Tags = checkin.Tags,
            CreatedAt = checkin.CreatedAt,
            UpdatedAt = checkin.UpdatedAt
        };

        return checkinDto;
    }

    public async Task<CheckinDto?> UpdateCheckinAsync(Guid id, UpdateCheckinDto dto, Guid userId)
    {
        var checkin = await _context.Checkins
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (checkin == null)
            return null;

        if (dto.Mood.HasValue)
            checkin.Mood = dto.Mood.Value;
        if (dto.Stress.HasValue)
            checkin.Stress = dto.Stress.Value;
        if (dto.Sleep.HasValue)
            checkin.Sleep = dto.Sleep.Value;
        if (dto.Notes != null)
            checkin.Notes = dto.Notes;
        if (dto.Tags != null)
            checkin.Tags = dto.Tags;

        checkin.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Retornar DTO com valores atualizados
        return new CheckinDto
        {
            Id = checkin.Id,
            UserId = checkin.UserId,
            Mood = checkin.Mood,
            Stress = checkin.Stress,
            Sleep = checkin.Sleep,
            Notes = checkin.Notes,
            Tags = checkin.Tags,
            CreatedAt = checkin.CreatedAt,
            UpdatedAt = checkin.UpdatedAt
        };
    }

    public async Task<bool> DeleteCheckinAsync(Guid id, Guid userId)
    {
        var checkin = await _context.Checkins
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (checkin == null)
            return false;

        _context.Checkins.Remove(checkin);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<WeeklyReportDto> GetWeeklyReportAsync(Guid userId, DateTime weekStart)
    {
        // Incluir até o final do último dia da semana (23:59:59.9999999)
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);

        var checkins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt >= weekStart && 
                       c.CreatedAt <= weekEnd)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var dailyData = checkins
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new DailyDataDto
            {
                Date = g.Key,
                Mood = (int)Math.Round(g.Average(c => c.Mood)),
                Stress = (int)Math.Round(g.Average(c => c.Stress)),
                Sleep = (int)Math.Round(g.Average(c => c.Sleep))
            })
            .ToList();

        var averages = new AveragesDto
        {
            Mood = checkins.Any() ? checkins.Average(c => c.Mood) : 0,
            Stress = checkins.Any() ? checkins.Average(c => c.Stress) : 0,
            Sleep = checkins.Any() ? checkins.Average(c => c.Sleep) : 0
        };

        return new WeeklyReportDto
        {
            UserId = userId,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Averages = averages,
            DailyData = dailyData
        };
    }

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(Guid userId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var checkins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt >= monthStart && 
                       c.CreatedAt <= monthEnd)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        // Mês anterior para comparação
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthEnd = monthStart.AddDays(-1);
        
        var previousMonthCheckins = await _context.Checkins
            .Where(c => c.UserId == userId && 
                       c.CreatedAt >= previousMonthStart && 
                       c.CreatedAt <= previousMonthEnd)
            .ToListAsync();

        var averages = checkins.Any() 
            ? new AveragesDto
            {
                Mood = checkins.Average(c => c.Mood),
                Stress = checkins.Average(c => c.Stress),
                Sleep = checkins.Average(c => c.Sleep)
            }
            : new AveragesDto();

        var previousMonthAverages = previousMonthCheckins.Any()
            ? new AveragesDto
            {
                Mood = previousMonthCheckins.Average(c => c.Mood),
                Stress = previousMonthCheckins.Average(c => c.Stress),
                Sleep = previousMonthCheckins.Average(c => c.Sleep)
            }
            : null;

        // Resumos semanais
        var weeklySummaries = new List<WeeklySummaryDto>();
        var currentWeekStart = monthStart;
        int weekNumber = 1;

        while (currentWeekStart <= monthEnd)
        {
            var weekEnd = currentWeekStart.AddDays(6);
            if (weekEnd > monthEnd) weekEnd = monthEnd;

            var weekCheckins = checkins
                .Where(c => c.CreatedAt.Date >= currentWeekStart.Date && 
                           c.CreatedAt.Date <= weekEnd.Date)
                .ToList();

            weeklySummaries.Add(new WeeklySummaryDto
            {
                WeekNumber = weekNumber,
                WeekStart = currentWeekStart,
                WeekEnd = weekEnd,
                Averages = weekCheckins.Any()
                    ? new AveragesDto
                    {
                        Mood = weekCheckins.Average(c => c.Mood),
                        Stress = weekCheckins.Average(c => c.Stress),
                        Sleep = weekCheckins.Average(c => c.Sleep)
                    }
                    : new AveragesDto(),
                CheckinCount = weekCheckins.Count
            });

            currentWeekStart = weekEnd.AddDays(1);
            weekNumber++;
        }

        // Melhor e pior dia
        DaySummaryDto? bestDay = null;
        DaySummaryDto? worstDay = null;

        if (checkins.Any())
        {
            var dailyGroups = checkins
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new DaySummaryDto
                {
                    Date = g.Key,
                    Mood = (int)Math.Round(g.Average(c => c.Mood)),
                    Stress = (int)Math.Round(g.Average(c => c.Stress)),
                    Sleep = (int)Math.Round(g.Average(c => c.Sleep)),
                    OverallScore = (g.Average(c => c.Mood) + (5 - g.Average(c => c.Stress)) + g.Average(c => c.Sleep)) / 3
                })
                .OrderByDescending(d => d.OverallScore)
                .ToList();

            bestDay = dailyGroups.FirstOrDefault();
            worstDay = dailyGroups.LastOrDefault();
        }

        var totalDays = (monthEnd - monthStart).Days + 1;
        var daysWithCheckin = checkins.Select(c => c.CreatedAt.Date).Distinct().Count();
        var checkinFrequency = totalDays > 0 ? (double)daysWithCheckin / totalDays * 100 : 0;

        return new MonthlyReportDto
        {
            UserId = userId,
            Year = year,
            Month = month,
            MonthStart = monthStart,
            MonthEnd = monthEnd,
            Averages = averages,
            PreviousMonthAverages = previousMonthAverages,
            WeeklySummaries = weeklySummaries,
            BestWorstDays = new BestWorstDaysDto
            {
                BestDay = bestDay,
                WorstDay = worstDay
            },
            TotalCheckins = checkins.Count,
            CheckinFrequency = Math.Round(checkinFrequency, 2)
        };
    }
}

