using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V2;

/// <summary>
/// Controller V2 para Check-ins
/// Versão 2 com melhorias e novas funcionalidades
/// </summary>
[ApiController]
[Route("api/v2/checkins")]
[Authorize]
[ApiExplorerSettings(GroupName = "v2")]
public class CheckinsController : ControllerBase
{
    private readonly ICheckinService _checkinService;
    private readonly ILogger<CheckinsController> _logger;

    public CheckinsController(ICheckinService checkinService, ILogger<CheckinsController> logger)
    {
        _checkinService = checkinService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID not found"));
    }

    /// <summary>
    /// Lista todos os check-ins do usuário autenticado com paginação (V2)
    /// </summary>
    /// <remarks>
    /// Versão 2: Melhorias na paginação e ordenação
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<CheckinDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<CheckinDto>>> GetCheckins(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var userId = GetUserId();
        var result = await _checkinService.GetCheckinsAsync(userId, page, pageSize);

        _logger.LogInformation("V2: Retrieved {Count} checkins for user {UserId} on page {Page}", 
            result.Data.Count, userId, page);

        return Ok(result);
    }

    /// <summary>
    /// Busca um check-in específico por ID (V2)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CheckinDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckinDto>> GetCheckin(Guid id)
    {
        var userId = GetUserId();
        var checkin = await _checkinService.GetCheckinByIdAsync(id, userId);

        if (checkin == null)
        {
            _logger.LogWarning("V2: Checkin {CheckinId} not found for user {UserId}", id, userId);
            return NotFound(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Check-in not found"
            });
        }

        return Ok(new ApiResponseDto<CheckinDto>
        {
            Success = true,
            Data = checkin
        });
    }

    /// <summary>
    /// Cria um novo check-in (V2)
    /// </summary>
    /// <remarks>
    /// Versão 2: Mantém compatibilidade com V1
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<CheckinDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckinDto>> CreateCheckin([FromBody] CreateCheckinDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var userId = GetUserId();
        var checkin = await _checkinService.CreateCheckinAsync(dto, userId);

        _logger.LogInformation("V2: Created checkin {CheckinId} for user {UserId}", checkin.Id, userId);

        return CreatedAtAction(
            nameof(GetCheckin),
            new { id = checkin.Id },
            new ApiResponseDto<CheckinDto>
            {
                Success = true,
                Data = checkin,
                Message = "Check-in created successfully"
            });
    }
}

