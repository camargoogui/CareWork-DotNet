using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V1;

/// <summary>
/// Controller V1 para Insights e Análises de Bem-estar
/// </summary>
[ApiController]
[Route("api/v1/insights")]
[Authorize]
[ApiExplorerSettings(GroupName = "v1")]
public class InsightsController : ControllerBase
{
    private readonly IInsightsService _insightsService;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(IInsightsService insightsService, ILogger<InsightsController> logger)
    {
        _insightsService = insightsService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID not found"));
    }

    /// <summary>
    /// 📈 Obtém análise de tendências do usuário
    /// </summary>
    /// <remarks>
    /// Analisa os check-ins do usuário em um período específico e identifica tendências de melhora, declínio ou estabilidade em humor, stress e qualidade do sono.
    /// 
    /// **Períodos disponíveis:**
    /// - `week`: Últimos 7 dias (padrão)
    /// - `month`: Últimos 30 dias
    /// - `year`: Últimos 365 dias
    /// 
    /// **Análises realizadas:**
    /// - **Média** de cada métrica no período
    /// - **Tendência**: "improving" (melhorando), "declining" (piorando), "stable" (estável)
    /// - **Percentual de mudança** em relação ao início do período
    /// - **Insights automáticos** baseados nas tendências identificadas
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// GET /api/v1/insights/trends?period=week
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///     "period": "week",
    ///     "startDate": "2025-11-07T00:00:00Z",
    ///     "endDate": "2025-11-14T23:59:59Z",
    ///     "mood": {
    ///       "average": 3.7,
    ///       "trend": "improving",
    ///       "changePercentage": 5.56,
    ///       "bestDay": null,
    ///       "worstDay": null
    ///     },
    ///     "stress": {
    ///       "average": 2.2,
    ///       "trend": "declining",
    ///       "changePercentage": -53.33
    ///     },
    ///     "sleep": {
    ///       "average": 3.45,
    ///       "trend": "declining",
    ///       "changePercentage": -13.51
    ///     },
    ///     "insights": [
    ///       "Seu humor está melhorando! Continue assim.",
    ///       "Ótimo! Seu nível de stress está diminuindo.",
    ///       "Sua qualidade de sono precisa de atenção."
    ///     ],
    ///     "alerts": []
    ///   }
    /// }
    /// ```
    /// 
    /// **Requisitos mínimos:**
    /// - Para análise semanal: pelo menos 3 check-ins nos últimos 7 dias
    /// - Para análise mensal: pelo menos 7 check-ins nos últimos 30 dias
    /// - Para análise anual: pelo menos 30 check-ins nos últimos 365 dias
    /// 
    /// **Nota:** Se não houver dados suficientes, a resposta indicará "Não há dados suficientes para análise".
    /// </remarks>
    /// <param name="period">Período de análise: "week" (padrão), "month" ou "year"</param>
    /// <returns>Análise completa de tendências com médias, percentuais de mudança e insights automáticos</returns>
    /// <response code="200">Análise de tendências retornada com sucesso</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(ApiResponseDto<TrendsInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TrendsInsightDto>> GetTrends([FromQuery] string period = "week")
    {
        var userId = GetUserId();
        var trends = await _insightsService.GetTrendsAsync(userId, period);

        _logger.LogInformation("Retrieved trends for user {UserId} - period {Period}", userId, period);

        return Ok(new ApiResponseDto<TrendsInsightDto>
        {
            Success = true,
            Data = trends
        });
    }

    /// <summary>
    /// 🔥 Obtém streak (sequência) de check-ins consecutivos
    /// </summary>
    /// <remarks>
    /// Calcula a sequência atual e a maior sequência histórica de dias consecutivos com check-ins.
    /// 
    /// **O que é um streak?**
    /// - Um streak é uma sequência de dias consecutivos em que o usuário fez pelo menos um check-in
    /// - O streak atual é quebrado se o usuário não fizer check-in em um dia
    /// - O streak mais longo é o maior número de dias consecutivos já alcançado
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// GET /api/v1/insights/streak
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "currentStreak": 5,
    ///     "longestStreak": 10,
    ///     "lastCheckinDate": "2025-11-14T10:00:00Z"
    ///   }
    /// }
    /// ```
    /// 
    /// **Interpretação:**
    /// - `currentStreak`: Sequência atual de dias consecutivos (5 dias seguidos fazendo check-in)
    /// - `longestStreak`: Maior sequência já alcançada (10 dias foi o recorde)
    /// - `lastCheckinDate`: Data do último check-in registrado
    /// 
    /// **Dica:** Use este endpoint para motivar o usuário a manter a consistência nos check-ins diários!
    /// </remarks>
    /// <returns>Informações sobre sequências de check-ins consecutivos</returns>
    /// <response code="200">Dados de streak retornados com sucesso</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("streak")]
    [ProducesResponseType(typeof(ApiResponseDto<StreakDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StreakDto>> GetStreak()
    {
        var userId = GetUserId();
        var streak = await _insightsService.GetStreakAsync(userId);

        _logger.LogInformation("Retrieved streak for user {UserId} - current: {Streak}", userId, streak.CurrentStreak);

        return Ok(new ApiResponseDto<StreakDto>
        {
            Success = true,
            Data = streak
        });
    }

    /// <summary>
    /// ⚖️ Compara dois períodos de check-ins
    /// </summary>
    /// <remarks>
    /// Compara as médias de humor, stress e sono entre dois períodos diferentes, permitindo identificar melhorias ou declínios ao longo do tempo.
    /// 
    /// **Parâmetros obrigatórios:**
    /// - `start1`: Data de início do período 1 (formato: YYYY-MM-DD)
    /// - `end1`: Data de fim do período 1 (formato: YYYY-MM-DD)
    /// - `start2`: Data de início do período 2 (formato: YYYY-MM-DD)
    /// - `end2`: Data de fim do período 2 (formato: YYYY-MM-DD)
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// GET /api/v1/insights/compare?start1=2025-11-01&end1=2025-11-07&start2=2025-11-08&end2=2025-11-14
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "period1": {
    ///       "startDate": "2025-11-01T00:00:00Z",
    ///       "endDate": "2025-11-07T23:59:59Z",
    ///       "moodAverage": 3.2,
    ///       "stressAverage": 3.5,
    ///       "sleepAverage": 3.0
    ///     },
    ///     "period2": {
    ///       "startDate": "2025-11-08T00:00:00Z",
    ///       "endDate": "2025-11-14T23:59:59Z",
    ///       "moodAverage": 3.7,
    ///       "stressAverage": 2.2,
    ///       "sleepAverage": 3.45
    ///     },
    ///     "differences": {
    ///       "mood": 0.5,
    ///       "stress": -1.3,
    ///       "sleep": 0.45
    ///     }
    ///   }
    /// }
    /// ```
    /// 
    /// **Interpretação das diferenças:**
    /// - **Valores positivos**: Melhora no período 2 em relação ao período 1
    /// - **Valores negativos**: Piora no período 2 em relação ao período 1
    /// - **Exemplo**: `stress: -1.3` significa que o stress diminuiu 1.3 pontos (melhorou!)
    /// 
    /// **Casos de uso:**
    /// - Comparar esta semana com a semana passada
    /// - Comparar este mês com o mês anterior
    /// - Avaliar o impacto de mudanças no estilo de vida
    /// </remarks>
    /// <param name="start1">Data de início do período 1 (YYYY-MM-DD)</param>
    /// <param name="end1">Data de fim do período 1 (YYYY-MM-DD)</param>
    /// <param name="start2">Data de início do período 2 (YYYY-MM-DD)</param>
    /// <param name="end2">Data de fim do período 2 (YYYY-MM-DD)</param>
    /// <returns>Comparação detalhada entre os dois períodos com médias e diferenças</returns>
    /// <response code="200">Comparação realizada com sucesso</response>
    /// <response code="400">Datas inválidas ou período sem check-ins suficientes</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("compare")]
    [ProducesResponseType(typeof(ApiResponseDto<ComparisonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ComparisonDto>> ComparePeriods(
        [FromQuery] DateTime start1,
        [FromQuery] DateTime end1,
        [FromQuery] DateTime start2,
        [FromQuery] DateTime end2)
    {
        var userId = GetUserId();
        var comparison = await _insightsService.ComparePeriodsAsync(userId, start1, end1, start2, end2);

        _logger.LogInformation("Compared periods for user {UserId}", userId);

        return Ok(new ApiResponseDto<ComparisonDto>
        {
            Success = true,
            Data = comparison
        });
    }

    /// <summary>
    /// 💡 Obtém dicas recomendadas personalizadas baseadas no histórico
    /// </summary>
    /// <remarks>
    /// Analisa os últimos 7 dias de check-ins do usuário e recomenda até 5 dicas personalizadas baseadas nas áreas que precisam de atenção.
    /// 
    /// **Lógica de recomendação:**
    /// A API analisa as tendências e médias dos últimos 7 dias e identifica áreas problemáticas:
    /// 
    /// - **Sleep (Sono)**: Recomendado se qualidade do sono está ruim (≤ 3.0) ou piorando
    /// - **Mood (Humor)**: Recomendado se humor está baixo (≤ 3.0) ou piorando
    /// - **Stress**: Recomendado se stress está alto (≥ 3.5) ou aumentando
    /// - **Wellness**: Recomendado se tudo está bem (para manter o progresso)
    /// 
    /// **Priorização:**
    /// - Se há 1 área problemática: até 5 dicas dessa categoria
    /// - Se há 2 áreas problemáticas: 3 dicas de cada
    /// - Se há 3+ áreas problemáticas: 2 dicas de cada
    /// 
    /// **Exemplo de requisição:**
    /// ```
    /// GET /api/v1/insights/recommended-tips
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
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
    ///   ]
    /// }
    /// ```
    /// 
    /// **Consistência com Trends:**
    /// As recomendações são **consistentes** com a análise de tendências (`/insights/trends`). Se o trends indica que o sono está piorando, as dicas recomendadas incluirão dicas de Sleep.
    /// 
    /// **Fallback:**
    /// Se não houver check-ins recentes ou se tudo estiver bem, retorna dicas gerais de Wellness.
    /// </remarks>
    /// <returns>Lista de até 5 dicas personalizadas baseadas no estado atual do usuário</returns>
    /// <response code="200">Lista de dicas recomendadas retornada com sucesso</response>
    /// <response code="401">Não autenticado - token JWT inválido ou ausente</response>
    [HttpGet("recommended-tips")]
    [ProducesResponseType(typeof(ApiResponseDto<List<TipDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TipDto>>> GetRecommendedTips()
    {
        var userId = GetUserId();
        var tips = await _insightsService.GetRecommendedTipsAsync(userId);

        _logger.LogInformation("Retrieved {Count} recommended tips for user {UserId}", tips.Count, userId);

        return Ok(new ApiResponseDto<List<TipDto>>
        {
            Success = true,
            Data = tips
        });
    }
}

