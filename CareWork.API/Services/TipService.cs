using Microsoft.EntityFrameworkCore;
using CareWork.Infrastructure.Models;
using CareWork.API.Models.DTOs;
using CareWork.Infrastructure.Data;
using AutoMapper;

namespace CareWork.API.Services;

public class TipService : ITipService
{
    private readonly CareWorkDbContext _context;
    private readonly IMapper _mapper;

    public TipService(CareWorkDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResponseDto<TipDto>> GetTipsAsync(int page, int pageSize, string? category = null)
    {
        var query = _context.Tips.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category != null && t.Category.ToLower() == category.ToLower());
        }

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var tips = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var tipDtos = _mapper.Map<List<TipDto>>(tips);

        return new PagedResponseDto<TipDto>
        {
            Data = tipDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
            Links = new LinksDto
            {
                Self = $"/api/v1/tips?page={page}&pageSize={pageSize}",
                First = $"/api/v1/tips?page=1&pageSize={pageSize}",
                Last = $"/api/v1/tips?page={totalPages}&pageSize={pageSize}",
                Previous = page > 1 ? $"/api/v1/tips?page={page - 1}&pageSize={pageSize}" : null,
                Next = page < totalPages ? $"/api/v1/tips?page={page + 1}&pageSize={pageSize}" : null
            }
        };
    }

    public async Task<TipDto?> GetTipByIdAsync(Guid id)
    {
        var tip = await _context.Tips.FindAsync(id);
        return tip == null ? null : _mapper.Map<TipDto>(tip);
    }

    public async Task<TipDto> CreateTipAsync(CreateTipDto dto)
    {
        var tip = new Tip
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Icon = dto.Icon,
            Color = dto.Color,
            Category = dto.Category,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tips.Add(tip);
        await _context.SaveChangesAsync();

        return _mapper.Map<TipDto>(tip);
    }

    public async Task<TipDto?> UpdateTipAsync(Guid id, UpdateTipDto dto)
    {
        var tip = await _context.Tips.FindAsync(id);

        if (tip == null)
            return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            tip.Title = dto.Title;
        if (!string.IsNullOrWhiteSpace(dto.Description))
            tip.Description = dto.Description;
        if (dto.Icon != null)
            tip.Icon = dto.Icon;
        if (dto.Color != null)
            tip.Color = dto.Color;
        if (dto.Category != null)
            tip.Category = dto.Category;

        tip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<TipDto>(tip);
    }

    public async Task<bool> DeleteTipAsync(Guid id)
    {
        var tip = await _context.Tips.FindAsync(id);

        if (tip == null)
            return false;

        _context.Tips.Remove(tip);
        await _context.SaveChangesAsync();

        return true;
    }
}

