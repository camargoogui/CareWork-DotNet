using CareWork.API.Models.DTOs;

namespace CareWork.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task<bool> UpdatePasswordAsync(Guid userId, UpdatePasswordDto dto);
    Task<bool> DeleteAccountAsync(Guid userId, string password);
    string GenerateJwtToken(Guid userId, string email);
}

