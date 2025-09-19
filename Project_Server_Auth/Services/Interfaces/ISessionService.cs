// Services/Interfaces/ISessionService.cs
using Project_Server_Auth.Dtos;

namespace Project_Server_Auth.Services.Interfaces
{
    public interface ISessionService
    {
        Task<string> CreateSessionAsync(string userId, string refreshToken, DateTime expiresAt, string? ipAddress = null, string? userAgent = null);
        Task<UserSessionDto?> GetSessionByRefreshTokenAsync(string refreshToken);
        Task<List<UserSessionDto>> GetUserSessionsAsync(string userId);
        Task<bool> RevokeSessionAsync(string refreshToken);
        Task<bool> RevokeAllUserSessionsAsync(string userId);
        Task<bool> RevokeSessionByIdAsync(int sessionId);
        Task<bool> UpdateSessionAsync(int sessionId, UpdateSessionDto updateSessionDto);
        Task<int> CleanupExpiredSessionsAsync();
        Task<bool> IsSessionValidAsync(string refreshToken);
    }
}