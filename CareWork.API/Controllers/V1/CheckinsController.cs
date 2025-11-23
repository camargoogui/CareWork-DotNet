using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V1;

/// <summary>
/// Controller V1 para Gerenciamento de Check-ins
/// </summary>
[ApiController]
[Route("api/v1/checkins")]
[Authorize]
[ApiExplorerSettings(GroupName = "v1")]
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
    /// 📋 Lista todos os check-ins do usuário autenticado com paginação
    /// </summary>
    /// <remarks>
    /// Retorna uma lista paginada de check-ins ordenados por data (mais recentes primeiro).
    /// Inclui links HATEOAS para navegação entre páginas.
    /// </remarks>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <returns>Lista paginada de check-ins com metadados de paginação</returns>
    /// <response code="200">Lista de check-ins retornada com sucesso</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<CheckinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponseDto<CheckinDto>>> GetCheckins(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var userId = GetUserId();
        var result = await _checkinService.GetCheckinsAsync(userId, page, pageSize);

        _logger.LogInformation("Retrieved {Count} checkins for user {UserId} on page {Page}", 
            result.Data.Count, userId, page);

        return Ok(result);
    }

    /// <summary>
    /// 🔍 Busca um check-in específico por ID
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes completos de um check-in específico do usuário autenticado.
    /// </remarks>
    /// <param name="id">ID único do check-in (GUID)</param>
    /// <returns>Detalhes completos do check-in</returns>
    /// <response code="200">Check-in encontrado e retornado com sucesso</response>
    /// <response code="404">Check-in não encontrado ou não pertence ao usuário autenticado</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<CheckinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckinDto>> GetCheckin(Guid id)
    {
        var userId = GetUserId();
        var checkin = await _checkinService.GetCheckinByIdAsync(id, userId);

        if (checkin == null)
        {
            _logger.LogWarning("Checkin {CheckinId} not found for user {UserId}", id, userId);
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
    /// ➕ Cria um novo check-in emocional
    /// </summary>
    /// <remarks>
    /// Registra um novo check-in com avaliações de humor (1-5), stress (1-5) e qualidade do sono (1-5).
    /// Campos opcionais: notes (texto) e tags (array de strings).
    /// </remarks>
    /// <param name="dto">Dados do check-in (mood, stress, sleep obrigatórios; notes e tags opcionais)</param>
    /// <returns>Check-in criado com sucesso, incluindo ID gerado e timestamp</returns>
    /// <response code="201">Check-in criado com sucesso. Retorna dados completos do check-in criado.</response>
    /// <response code="400">Dados inválidos. Verifique se mood, stress e sleep estão entre 1 e 5.</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente. Faça login primeiro.</response>
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

        // Log dos valores recebidos para debug
        _logger.LogInformation("Creating checkin - Mood: {Mood}, Stress: {Stress}, Sleep: {Sleep}", 
            dto.Mood, dto.Stress, dto.Sleep);

        var userId = GetUserId();
        var checkin = await _checkinService.CreateCheckinAsync(dto, userId);

        // Log dos valores retornados para debug
        _logger.LogInformation("Created checkin {CheckinId} for user {UserId} - Mood: {Mood}, Stress: {Stress}, Sleep: {Sleep}", 
            checkin.Id, userId, checkin.Mood, checkin.Stress, checkin.Sleep);

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

    /// <summary>
    /// ✏️ Atualiza um check-in existente
    /// </summary>
    /// <remarks>
    /// Atualiza os dados de um check-in do usuário autenticado. Todos os campos são opcionais.
    /// </remarks>
    /// <param name="id">ID único do check-in a ser atualizado (GUID)</param>
    /// <param name="dto">Dados atualizados do check-in (todos os campos são opcionais)</param>
    /// <returns>Check-in atualizado com sucesso</returns>
    /// <response code="200">Check-in atualizado com sucesso</response>
    /// <response code="404">Check-in não encontrado ou não pertence ao usuário autenticado</response>
    /// <response code="400">Dados inválidos. Verifique os valores informados.</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<CheckinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckinDto>> UpdateCheckin(Guid id, [FromBody] UpdateCheckinDto dto)
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
        var checkin = await _checkinService.UpdateCheckinAsync(id, dto, userId);

        if (checkin == null)
        {
            _logger.LogWarning("Checkin {CheckinId} not found for update by user {UserId}", id, userId);
            return NotFound(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Check-in not found"
            });
        }

        _logger.LogInformation("Updated checkin {CheckinId} for user {UserId}", id, userId);

        return Ok(new ApiResponseDto<CheckinDto>
        {
            Success = true,
            Data = checkin,
            Message = "Check-in updated successfully"
        });
    }

    /// <summary>
    /// 🗑️ Deleta um check-in permanentemente
    /// </summary>
    /// <remarks>
    /// Remove permanentemente um check-in do sistema. Ação irreversível.
    /// </remarks>
    /// <param name="id">ID único do check-in a ser deletado (GUID)</param>
    /// <returns>Nenhum conteúdo (204 No Content)</returns>
    /// <response code="204">Check-in deletado com sucesso</response>
    /// <response code="404">Check-in não encontrado ou não pertence ao usuário autenticado</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCheckin(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _checkinService.DeleteCheckinAsync(id, userId);

        if (!deleted)
        {
            _logger.LogWarning("Checkin {CheckinId} not found for deletion by user {UserId}", id, userId);
            return NotFound(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Check-in not found"
            });
        }

        _logger.LogInformation("Deleted checkin {CheckinId} for user {UserId}", id, userId);

        return NoContent();
    }
}

