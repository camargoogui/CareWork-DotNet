using CareWork.API.Models.DTOs;

namespace CareWork.API.Services;

public interface ITipService
{
    Task<PagedResponseDto<TipDto>> GetTipsAsync(int page, int pageSize, string? category = null);
    Task<TipDto?> GetTipByIdAsync(Guid id);
    Task<TipDto> CreateTipAsync(CreateTipDto dto);
    Task<TipDto?> UpdateTipAsync(Guid id, UpdateTipDto dto);
    Task<bool> DeleteTipAsync(Guid id);
}

