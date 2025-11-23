using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V1;

/// <summary>
/// Controller V1 para Gerenciamento de Tips (Dicas de Bem-estar)
/// </summary>
[ApiController]
[Route("api/v1/tips")]
[Authorize]
[ApiExplorerSettings(GroupName = "v1")]
public class TipsController : ControllerBase
{
    private readonly ITipService _tipService;
    private readonly ILogger<TipsController> _logger;

    public TipsController(ITipService tipService, ILogger<TipsController> logger)
    {
        _tipService = tipService;
        _logger = logger;
    }

    /// <summary>
    /// 📚 Lista todas as dicas de bem-estar com paginação e filtro por categoria
    /// </summary>
    /// <remarks>
    /// Retorna lista paginada de dicas. Filtro opcional por categoria: Stress, Sleep, Mood, Wellness.
    /// </remarks>
    /// {
    ///   "data": [
    ///     {
    ///       "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "title": "Técnicas de Respiração Profunda",
    ///       "description": "Pratique respiração profunda por 5 minutos: inspire por 4 segundos, segure por 4, expire por 6. Isso ajuda a reduzir o stress imediatamente.",
    ///       "icon": "breath",
    ///       "color": "#FF5722",
    ///       "category": "Stress",
    ///       "createdAt": "2025-11-14T10:00:00Z",
    ///       "updatedAt": null
    ///     }
    ///   ],
    ///   "page": 1,
    ///   "pageSize": 10,
    ///   "totalCount": 20,
    ///   "totalPages": 2,
    ///   "hasPreviousPage": false,
    ///   "hasNextPage": true,
    ///   "links": { ... }
    /// }
    /// ```
    /// 
    /// **Nota:** O sistema já vem pré-populado com 20 dicas iniciais categorizadas.
    /// </remarks>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="category">Filtro opcional por categoria: "Stress", "Sleep", "Mood" ou "Wellness"</param>
    /// <returns>Lista paginada de dicas com metadados de paginação</returns>
    /// <response code="200">Lista de dicas retornada com sucesso</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<TipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponseDto<TipDto>>> GetTips(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _tipService.GetTipsAsync(page, pageSize, category);

        _logger.LogInformation("Retrieved {Count} tips on page {Page}", result.Data.Count, page);

        return Ok(result);
    }

    /// <summary>
    /// 🔍 Busca uma dica específica por ID
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes completos de uma dica específica por ID.
    /// </remarks>
    /// <param name="id">ID único da dica (GUID)</param>
    /// <returns>Detalhes completos da dica</returns>
    /// <response code="200">Dica encontrada e retornada com sucesso</response>
    /// <response code="404">Dica não encontrada</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<TipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TipDto>> GetTip(Guid id)
    {
        var tip = await _tipService.GetTipByIdAsync(id);

        if (tip == null)
        {
            _logger.LogWarning("Tip {TipId} not found", id);
            return NotFound(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Tip not found"
            });
        }

        return Ok(new ApiResponseDto<TipDto>
        {
            Success = true,
            Data = tip
        });
    }

    /// <summary>
    /// ➕ Cria uma nova dica de bem-estar
    /// </summary>
    /// <remarks>
    /// Cria uma nova dica. Campos obrigatórios: title, description, category (Stress/Sleep/Mood/Wellness).
    /// Opcionais: icon, color.
    /// </remarks>
    /// <param name="dto">Dados da dica (title, description, category obrigatórios)</param>
    /// <returns>Dica criada com sucesso, incluindo ID gerado</returns>
    /// <response code="201">Dica criada com sucesso</response>
    /// <response code="400">Dados inválidos. Verifique se todos os campos obrigatórios foram informados.</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<TipDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TipDto>> CreateTip([FromBody] CreateTipDto dto)
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

        var tip = await _tipService.CreateTipAsync(dto);

        _logger.LogInformation("Created tip {TipId}", tip.Id);

        return CreatedAtAction(
            nameof(GetTip),
            new { id = tip.Id },
            new ApiResponseDto<TipDto>
            {
                Success = true,
                Data = tip,
                Message = "Tip created successfully"
            });
    }

    /// <summary>
    /// ✏️ Atualiza uma dica existente
    /// </summary>
    /// <remarks>
    /// Atualiza os dados de uma dica existente. Todos os campos são opcionais.
    /// </remarks>
    /// <param name="id">ID único da dica a ser atualizada (GUID)</param>
    /// <param name="dto">Dados atualizados da dica (todos os campos são opcionais)</param>
    /// <returns>Dica atualizada com sucesso</returns>
    /// <response code="200">Dica atualizada com sucesso</response>
    /// <response code="404">Dica não encontrada</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<TipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TipDto>> UpdateTip(Guid id, [FromBody] UpdateTipDto dto)
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

        var tip = await _tipService.UpdateTipAsync(id, dto);

        if (tip == null)
        {
            _logger.LogWarning("Tip {TipId} not found for update", id);
            return NotFound(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Tip not found"
            });
        }

        _logger.LogInformation("Updated tip {TipId}", id);

        return Ok(new ApiResponseDto<TipDto>
        {
            Success = true,
            Data = tip,
            Message = "Tip updated successfully"
        });
    }

    /// <summary>
    /// 🗑️ Deleta uma dica permanentemente
    /// </summary>
    /// <remarks>
    /// Remove permanentemente uma dica do sistema. Esta ação é **irreversível**.
    /// 
    /// **⚠️ Atenção:**
    /// - Esta ação não pode ser desfeita
    /// - A dica será removida permanentemente do banco de dados
    /// - Isso pode afetar recomendações que dependem desta dica
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// DELETE /api/v1/tips/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// ```
    /// 
    /// **Exemplo de resposta (204 No Content):**
    /// ```
    /// (sem conteúdo no corpo da resposta)
    /// ```
    /// </remarks>
    /// <param name="id">ID único da dica a ser deletada (GUID)</param>
    /// <returns>Nenhum conteúdo (204 No Content)</returns>
    /// <response code="204">Dica deletada com sucesso</response>
    /// <response code="404">Dica não encontrada</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTip(Guid id)
    {
        var deleted = await _tipService.DeleteTipAsync(id);

        if (!deleted)
        {
            _logger.LogWarning("Tip {TipId} not found for deletion", id);
            return NotFound(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Tip not found"
            });
        }

        _logger.LogInformation("Deleted tip {TipId}", id);

        return NoContent();
    }
}

