using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V2;

/// <summary>
/// Controller V2 para Autenticação
/// Versão 2 com melhorias na resposta e validações
/// </summary>
[ApiController]
[Route("api/v2/auth")]
[ApiExplorerSettings(GroupName = "v2")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID not found"));
    }

    /// <summary>
    /// Realiza login e retorna token JWT (V2)
    /// </summary>
    /// <remarks>
    /// Versão 2: Mantém compatibilidade com V1
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
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

        var result = await _authService.LoginAsync(dto);

        if (result == null)
        {
            _logger.LogWarning("V2: Failed login attempt for email {Email}", dto.Email);
            return Unauthorized(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        _logger.LogInformation("V2: User {UserId} logged in successfully", result.UserId);

        return Ok(new ApiResponseDto<AuthResponseDto>
        {
            Success = true,
            Data = result,
            Message = "Login successful"
        });
    }

    /// <summary>
    /// Registra um novo usuário (V2)
    /// </summary>
    /// <remarks>
    /// Versão 2: Mantém compatibilidade com V1
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponseDto<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
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

        try
        {
            var result = await _authService.RegisterAsync(dto);

            _logger.LogInformation("V2: User {UserId} registered successfully", result.UserId);

            return CreatedAtAction(
                nameof(Login),
                new { },
                new ApiResponseDto<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "User registered successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2: Error registering user with email {Email}", dto.Email);
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Error registering user. Email may already be in use.",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}

