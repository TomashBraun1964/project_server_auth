// Services/SessionService.cs
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Services
{
    public class SessionService : ISessionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionService> _logger;

        public SessionService(AppDbContext context, ILogger<SessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> CreateSessionAsync(string userId, string refreshToken, DateTime expiresAt, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var session = new UserSession
                {
                    UserId = userId,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    IsRevoked = false,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceInfo = ExtractDeviceInfo(userAgent),
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании сессии для пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<UserSessionDto?> GetSessionByRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

                return session != null ? MapToUserSessionDto(session) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессии по refresh токену");
                return null;
            }
        }

        public async Task<List<UserSessionDto>> GetUserSessionsAsync(string userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return sessions.Select(MapToUserSessionDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессий пользователя {UserId}", userId);
                return new List<UserSessionDto>();
            }
        }

        public async Task<bool> RevokeSessionAsync(string refreshToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && !s.IsRevoked);

                if (session == null)
                    return false;

                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве сессии по refresh токену");
                return false;
            }
        }

        public async Task<bool> RevokeAllUserSessionsAsync(string userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && !s.IsRevoked)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве всех сессий пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RevokeSessionByIdAsync(int sessionId)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked);

                if (session == null)
                    return false;

                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве сессии по ID {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> UpdateSessionAsync(int sessionId, UpdateSessionDto updateSessionDto)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                    return false;

                if (!string.IsNullOrEmpty(updateSessionDto.RefreshToken))
                    session.RefreshToken = updateSessionDto.RefreshToken;

                if (updateSessionDto.ExpiresAt.HasValue)
                    session.ExpiresAt = updateSessionDto.ExpiresAt.Value;

                if (updateSessionDto.IsRevoked.HasValue)
                {
                    session.IsRevoked = updateSessionDto.IsRevoked.Value;
                    if (updateSessionDto.IsRevoked.Value && session.RevokedAt == null)
                        session.RevokedAt = DateTime.UtcNow;
                }

                if (updateSessionDto.RevokedAt.HasValue)
                    session.RevokedAt = updateSessionDto.RevokedAt.Value;

                if (updateSessionDto.DeviceInfo != null)
                    session.DeviceInfo = updateSessionDto.DeviceInfo;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении сессии {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<int> CleanupExpiredSessionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredSessions = await _context.UserSessions
                    .Where(s => s.ExpiresAt <= now && !s.IsRevoked)
                    .ToListAsync();

                foreach (var session in expiredSessions)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = now;
                }

                await _context.SaveChangesAsync();
                return expiredSessions.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке истекших сессий");
                return 0;
            }
        }

        public async Task<bool> IsSessionValidAsync(string refreshToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

                if (session == null)
                    return false;

                return !session.IsRevoked && session.ExpiresAt > DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке валидности сессии");
                return false;
            }
        }

        private UserSessionDto MapToUserSessionDto(UserSession session)
        {
            return new UserSessionDto
            {
                Id = session.Id,
                RefreshToken = session.RefreshToken,
                ExpiresAt = session.ExpiresAt,
                IsRevoked = session.IsRevoked,
                RevokedAt = session.RevokedAt,
                DeviceInfo = session.DeviceInfo
            };
        }

        private string? ExtractDeviceInfo(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return null;

            // Простое извлечение информации о браузере и ОС
            var deviceInfo = userAgent;

            // Сокращаем до максимальной длины
            if (deviceInfo.Length > 500)
                deviceInfo = deviceInfo.Substring(0, 500);

            return deviceInfo;
        }
    }
}