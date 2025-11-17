using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;

namespace CareWork.API.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 🔐 Realiza login e retorna token JWT
    /// </summary>
    /// <remarks>
    /// Autentica um usuário existente e retorna um token JWT válido por 24 horas.
    /// 
    /// **Validações:**
    /// - Email deve ser válido e existir no sistema
    /// - Senha deve corresponder ao email informado
    /// 
    /// **Exemplo de requisição:**
    /// ```json
    /// POST /api/v1/auth/login
    /// {
    ///   "email": "usuario@example.com",
    ///   "password": "senha123"
    /// }
    /// ```
    /// 
    /// **Exemplo de resposta (200 OK):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///     "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///     "email": "usuario@example.com",
    ///     "name": "João Silva"
    ///   },
    ///   "message": "Login successful"
    /// }
    /// ```
    /// 
    /// **Após o login:**
    /// 1. Copie o `token` do campo `data.token`
    /// 2. Clique no botão **Authorize** no topo do Swagger
    /// 3. Cole o token no formato: `Bearer {seu-token}`
    /// 4. Agora você pode testar todos os endpoints autenticados
    /// </remarks>
    /// <param name="dto">Credenciais de login (email e senha obrigatórios)</param>
    /// <returns>Token JWT e informações do usuário autenticado</returns>
    /// <response code="200">Login realizado com sucesso. Retorna token JWT e dados do usuário.</response>
    /// <response code="401">Email ou senha inválidos. Verifique suas credenciais.</response>
    /// <response code="400">Dados inválidos. Verifique se o email está no formato correto e a senha foi informada.</response>
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
            _logger.LogWarning("Failed login attempt for email {Email}", dto.Email);
            return Unauthorized(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        _logger.LogInformation("User {UserId} logged in successfully", result.UserId);

        return Ok(new ApiResponseDto<AuthResponseDto>
        {
            Success = true,
            Data = result,
            Message = "Login successful"
        });
    }

    /// <summary>
    /// 📝 Registra um novo usuário e retorna token JWT
    /// </summary>
    /// <remarks>
    /// Cria uma nova conta de usuário no sistema e retorna automaticamente um token JWT para autenticação imediata.
    /// 
    /// **Validações:**
    /// - Email deve ser válido e único (não pode estar em uso)
    /// - Senha é obrigatória (será criptografada com BCrypt)
    /// - Nome deve conter apenas letras e espaços, com no mínimo 2 caracteres
    /// 
    /// **Exemplo de requisição:**
    /// ```json
    /// POST /api/v1/auth/register
    /// {
    ///   "email": "novo.usuario@example.com",
    ///   "password": "senhaSegura123",
    ///   "name": "João Silva"
    /// }
    /// ```
    /// 
    /// **Exemplo de resposta (201 Created):**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///     "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///     "email": "novo.usuario@example.com",
    ///     "name": "João Silva"
    ///   },
    ///   "message": "User registered successfully"
    /// }
    /// ```
    /// 
    /// **Após o registro:**
    /// - O token JWT já está disponível na resposta
    /// - Você pode usar este token imediatamente para autenticar outras requisições
    /// - Não é necessário fazer login após o registro
    /// 
    /// **Erros comuns:**
    /// - `400 Bad Request`: Email já está em uso ou dados inválidos
    /// - `400 Bad Request`: Nome contém caracteres inválidos (aceita apenas letras e espaços)
    /// </remarks>
    /// <param name="dto">Dados do novo usuário (email, password, name - todos obrigatórios)</param>
    /// <returns>Token JWT e informações do usuário criado</returns>
    /// <response code="201">Usuário registrado com sucesso. Retorna token JWT para autenticação imediata.</response>
    /// <response code="400">Dados inválidos ou email já está em uso por outro usuário.</response>
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

            _logger.LogInformation("User {UserId} registered successfully", result.UserId);

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
            _logger.LogError(ex, "Error registering user with email {Email}", dto.Email);
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Error registering user. Email may already be in use.",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID not found"));
    }

    /// <summary>
    /// Atualiza o perfil do usuário autenticado
    /// </summary>
    /// <remarks>
    /// Permite atualizar nome e email do usuário.
    /// 
    /// Exemplo de requisição:
    /// 
    ///     PUT /api/v1/auth/profile
    ///     {
    ///       "name": "João Silva",
    ///       "email": "novoemail@example.com"
    ///     }
    ///     
    /// **Importante:** O email não pode estar em uso por outro usuário.
    /// </remarks>
    /// <param name="dto">Novos dados do perfil</param>
    /// <returns>Dados atualizados do usuário</returns>
    /// <response code="200">Perfil atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou email já em uso</response>
    /// <response code="401">Não autenticado</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
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
            var userId = GetUserId();
            var user = await _authService.UpdateProfileAsync(userId, dto);

            if (user == null)
            {
                return NotFound(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            _logger.LogInformation("User {UserId} updated profile", userId);

            return Ok(new ApiResponseDto<UserDto>
            {
                Success = true,
                Data = user,
                Message = "Profile updated successfully"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Email already in use"))
        {
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Email already in use",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Invalid data",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user");
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Error updating profile",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Atualiza a senha do usuário autenticado
    /// </summary>
    /// <remarks>
    /// Requer a senha atual para confirmar a alteração.
    /// 
    /// Exemplo de requisição:
    /// 
    ///     PUT /api/v1/auth/password
    ///     {
    ///       "currentPassword": "senhaAtual123",
    ///       "newPassword": "novaSenha456"
    ///     }
    ///     
    /// **Importante:** A nova senha deve ser diferente da senha atual.
    /// </remarks>
    /// <param name="dto">Senha atual e nova senha</param>
    /// <returns>Confirmação de atualização</returns>
    /// <response code="200">Senha atualizada com sucesso</response>
    /// <response code="400">Dados inválidos ou senha atual incorreta</response>
    /// <response code="401">Não autenticado ou senha atual incorreta</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
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
            var userId = GetUserId();
            var updated = await _authService.UpdatePasswordAsync(userId, dto);

            if (!updated)
            {
                _logger.LogWarning("Failed password update attempt for user {UserId}", userId);
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Current password is incorrect"
                });
            }

            _logger.LogInformation("User {UserId} updated password", userId);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Password updated successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user");
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Error updating password",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Deleta a conta do usuário autenticado
    /// </summary>
    /// <remarks>
    /// **ATENÇÃO:** Esta ação é irreversível! Todos os dados do usuário serão permanentemente deletados, incluindo:
    /// - Perfil do usuário
    /// - Todos os check-ins
    /// - Histórico completo
    /// 
    /// Requer confirmação com a senha atual.
    /// 
    /// Exemplo de requisição:
    /// 
    ///     DELETE /api/v1/auth/account
    ///     {
    ///       "password": "senhaAtual123"
    ///     }
    /// </remarks>
    /// <param name="dto">Senha para confirmação</param>
    /// <returns>Confirmação de exclusão</returns>
    /// <response code="200">Conta deletada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Não autenticado ou senha incorreta</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
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
            var userId = GetUserId();
            var deleted = await _authService.DeleteAccountAsync(userId, dto.Password);

            if (!deleted)
            {
                _logger.LogWarning("Failed account deletion attempt for user {UserId}", userId);
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Password is incorrect or user not found"
                });
            }

            _logger.LogWarning("User {UserId} deleted their account", userId);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Account deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user");
            return BadRequest(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Error deleting account",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}

