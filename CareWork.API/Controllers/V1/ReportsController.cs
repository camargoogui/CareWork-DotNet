using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V1;

/// <summary>
/// Controller V1 para Relatórios Semanais e Mensais
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
[ApiExplorerSettings(GroupName = "v1")]
public class ReportsController : ControllerBase
{
    private readonly ICheckinService _checkinService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ICheckinService checkinService, ILogger<ReportsController> logger)
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
    /// 📊 Gera relatório semanal completo de check-ins
    /// </summary>
    /// <remarks>
    /// Gera um relatório detalhado de uma semana específica, incluindo dados diários, médias e insights automáticos.
    /// 
    /// **Parâmetros:**
    /// - `weekStart`: Data de início da semana (formato: YYYY-MM-DD) - **obrigatório**
    /// - `userId`: ID do usuário (opcional, padrão: usuário autenticado)
    /// 
    /// **O que o relatório inclui:**
    /// - Total de check-ins na semana
    /// - Médias de humor, stress e sono
    /// - Dados diários (um registro por dia com check-ins agregados)
    /// - Insights automáticos baseados nas médias
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// GET /api/v1/reports/weekly?weekStart=2025-11-07
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "weekStart": "2025-11-07T00:00:00Z",
    ///     "weekEnd": "2025-11-14T23:59:59Z",
    ///     "totalCheckins": 7,
    ///     "averageMood": 3.7,
    ///     "averageStress": 2.2,
    ///     "averageSleep": 3.45,
    ///     "dailyData": [
    ///       {
    ///         "date": "2025-11-07T00:00:00Z",
    ///         "mood": 3,
    ///         "stress": 3,
    ///         "sleep": 3,
    ///         "checkinCount": 1
    ///       }
    ///     ],
    ///     "insights": [
    ///       "Seu humor está melhorando! Continue assim.",
    ///       "Ótimo! Seu nível de stress está diminuindo."
    ///     ]
    ///   }
    /// }
    /// ```
    /// 
    /// **Nota:** A semana inclui 7 dias completos a partir de `weekStart` (incluindo o último dia).
    /// </remarks>
    /// <param name="weekStart">Data de início da semana (YYYY-MM-DD) - obrigatório</param>
    /// <param name="userId">ID do usuário (opcional, padrão: usuário autenticado)</param>
    /// <returns>Relatório semanal completo com dados diários, médias e insights</returns>
    /// <response code="200">Relatório semanal gerado com sucesso</response>
    /// <response code="400">Parâmetro weekStart ausente ou inválido</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    /// <response code="403">Tentativa de acessar relatório de outro usuário</response>
    [HttpGet("weekly")]
    [ProducesResponseType(typeof(ApiResponseDto<WeeklyReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WeeklyReportDto>> GetWeeklyReport(
        [FromQuery] DateTime weekStart,
        [FromQuery] Guid? userId = null)
    {
        var authenticatedUserId = GetUserId();
        var targetUserId = userId ?? authenticatedUserId;

        // Verifica se o usuário está tentando acessar dados de outro usuário
        if (targetUserId != authenticatedUserId)
        {
            _logger.LogWarning("User {UserId} attempted to access report for user {TargetUserId}", 
                authenticatedUserId, targetUserId);
            return Forbid();
        }

        if (weekStart == default)
        {
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "weekStart parameter is required"
            });
        }

        var report = await _checkinService.GetWeeklyReportAsync(targetUserId, weekStart);

        _logger.LogInformation("Generated weekly report for user {UserId} starting {WeekStart}", 
            targetUserId, weekStart);

        return Ok(new ApiResponseDto<WeeklyReportDto>
        {
            Success = true,
            Data = report
        });
    }

    /// <summary>
    /// 📅 Gera relatório mensal completo de check-ins
    /// </summary>
    /// <remarks>
    /// Gera um relatório detalhado de um mês específico, incluindo resumos semanais, médias mensais e insights automáticos.
    /// 
    /// **Parâmetros obrigatórios:**
    /// - `year`: Ano (ex: 2025)
    /// - `month`: Mês (1-12, onde 1 = Janeiro, 12 = Dezembro)
    /// 
    /// **O que o relatório inclui:**
    /// - Total de check-ins no mês
    /// - Médias mensais de humor, stress e sono
    /// - Resumos semanais (4-5 semanas dependendo do mês)
    /// - Insights automáticos baseados nas médias mensais
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// GET /api/v1/reports/monthly?year=2025&month=11
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "year": 2025,
    ///     "month": 11,
    ///     "totalCheckins": 30,
    ///     "averageMood": 3.8,
    ///     "averageStress": 2.5,
    ///     "averageSleep": 3.6,
    ///     "weeklySummaries": [
    ///       {
    ///         "weekStart": "2025-11-01T00:00:00Z",
    ///         "weekEnd": "2025-11-07T23:59:59Z",
    ///         "checkinCount": 7,
    ///         "averageMood": 3.7,
    ///         "averageStress": 2.2,
    ///         "averageSleep": 3.45
    ///       }
    ///     ],
    ///     "insights": [
    ///       "Novembro foi um mês positivo!",
    ///       "Sua consistência melhorou este mês."
    ///     ]
    ///   }
    /// }
    /// ```
    /// 
    /// **Validações:**
    /// - Mês deve estar entre 1 e 12
    /// - Ano deve ser válido (ex: 2020-2099)
    /// </remarks>
    /// <param name="year">Ano do relatório (ex: 2025)</param>
    /// <param name="month">Mês do relatório (1-12, onde 1 = Janeiro, 12 = Dezembro)</param>
    /// <returns>Relatório mensal completo com resumos semanais, médias e insights</returns>
    /// <response code="200">Relatório mensal gerado com sucesso</response>
    /// <response code="400">Mês inválido (deve estar entre 1 e 12) ou parâmetros ausentes</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(ApiResponseDto<MonthlyReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MonthlyReportDto>> GetMonthlyReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var authenticatedUserId = GetUserId();

        if (month < 1 || month > 12)
        {
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Month must be between 1 and 12"
            });
        }

        var report = await _checkinService.GetMonthlyReportAsync(authenticatedUserId, year, month);

        _logger.LogInformation("Generated monthly report for user {UserId} - {Year}/{Month}", 
            authenticatedUserId, year, month);

        return Ok(new ApiResponseDto<MonthlyReportDto>
        {
            Success = true,
            Data = report
        });
    }
}

