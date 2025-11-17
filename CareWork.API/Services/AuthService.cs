using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareWork.Infrastructure.Models;
using CareWork.API.Models.DTOs;
using CareWork.Infrastructure.Data;

namespace CareWork.API.Services;

public class AuthService : IAuthService
{
    private readonly CareWorkDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(CareWorkDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var normalizedEmail = dto.Email.ToLowerInvariant().Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user.Id, user.Email);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Normalizar email antes de verificar
        var normalizedEmail = dto.Email.ToLowerInvariant().Trim();
        
        // Verificar se o email já existe
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already in use");
        }

        // Validar nome (não pode ser "string" ou apenas espaços)
        var trimmedName = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) || 
            trimmedName.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Name must be a valid name, not a placeholder");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Name = trimmedName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user.Id, user.Email);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name
        };
    }

    public string GenerateJwtToken(Guid userId, string email)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
        var issuer = _configuration["Jwt:Issuer"] ?? "CareWork";
        var audience = _configuration["Jwt:Audience"] ?? "CareWork";
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440"); // 24 horas

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return null;

        // Verificar se o novo email já está em uso por outro usuário
        if (dto.Email.ToLowerInvariant().Trim() != user.Email.ToLowerInvariant())
        {
            var normalizedEmail = dto.Email.ToLowerInvariant().Trim();
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Id != userId);
            
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already in use");
            }
        }

        // Validar nome
        var trimmedName = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) || 
            trimmedName.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Name must be a valid name, not a placeholder");
        }

        // Atualizar dados
        user.Name = trimmedName;
        user.Email = dto.Email.ToLowerInvariant().Trim();

        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdatePasswordAsync(Guid userId, UpdatePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return false;

        // Verificar senha atual
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        // Verificar se a nova senha é diferente da atual
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
        {
            throw new ArgumentException("New password must be different from current password");
        }

        // Atualizar senha
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAccountAsync(Guid userId, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return false;

        // Verificar senha antes de deletar
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return false;
        }

        // Deletar check-ins do usuário primeiro (cascata manual)
        var checkins = await _context.Checkins
            .Where(c => c.UserId == userId)
            .ToListAsync();
        
        if (checkins.Any())
        {
            _context.Checkins.RemoveRange(checkins);
        }

        // Deletar usuário
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }
}

