// Services/Interfaces/IAuthService.cs
using Project_Server_Auth.Dtos;

namespace Project_Server_Auth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> LogoutAsync(string userId, string? refreshToken = null);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<UserProfileDto?> GetUserProfileAsync(string userId);
    }
}