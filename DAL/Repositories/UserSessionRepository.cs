
// DAL/Repositories/UserSessionRepository.cs
using DAL.Models;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с пользовательскими сессиями
    /// </summary>
    public class UserSessionRepository : Repository<UserSession>, IUserSessionRepository
    {
        public UserSessionRepository(DbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(string userId)
        {
            return await Entities
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserSession>> GetAllSessionsByUserIdAsync(string userId)
        {
            return await Entities
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await GetFirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);
        }

        public async Task RevokeAllUserSessionsAsync(string userId)
        {
            var sessions = await Entities
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            var revokedAt = DateTime.UtcNow;
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
                session.RevokedAt = revokedAt;
            }
        }

        public async Task RevokeSessionAsync(int sessionId)
        {
            var session = await GetByIdAsync(sessionId);
            if (session != null && session.IsActive)
            {
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                Update(session);
            }
        }

        public async Task<int> GetActiveSessionsCountAsync(string userId)
        {
            return await CountAsync(s => s.UserId == userId && s.IsActive);
        }

        public async Task RevokeOtherSessionsAsync(string userId, string currentRefreshToken)
        {
            var sessions = await Entities
                .Where(s => s.UserId == userId && s.IsActive && s.RefreshToken != currentRefreshToken)
                .ToListAsync();

            var revokedAt = DateTime.UtcNow;
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
                session.RevokedAt = revokedAt;
            }
        }

        public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync()
        {
            var now = DateTime.UtcNow;
            return await Entities
                .AsNoTracking()
                .Where(s => s.ExpiresAt <= now && !s.IsRevoked)
                .ToListAsync();
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await GetExpiredSessionsAsync();
            var revokedAt = DateTime.UtcNow;

            foreach (var session in expiredSessions)
            {
                var trackedSession = await GetByIdAsync(session.Id);
                if (trackedSession != null)
                {
                    trackedSession.IsRevoked = true;
                    trackedSession.RevokedAt = revokedAt;
                    Update(trackedSession);
                }
            }
        }

        public async Task<IEnumerable<UserSession>> GetSessionsByIpAddressAsync(string ipAddress)
        {
            return await Entities
                .AsNoTracking()
                .Where(s => s.IpAddress == ipAddress)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserSession>> GetRecentActiveSessionsAsync(int count = 100)
        {
            return await Entities
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> IsSessionLimitReachedAsync(string userId, int maxSessions)
        {
            var activeSessionsCount = await GetActiveSessionsCountAsync(userId);
            return activeSessionsCount >= maxSessions;
        }
    }
}