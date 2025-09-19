
// DAL/Repositories/ActivityLogRepository.cs
using DAL.Models;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с логами активности
    /// </summary>
    public class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
    {
        public ActivityLogRepository(DbContext context) : base(context)
        {
        }

        public async Task<DateTime?> GetLastActivityDateAsync(string userId)
        {
            var lastActivity = await Entities
                .AsNoTracking()
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.Timestamp)
                .Select(al => al.Timestamp)
                .FirstOrDefaultAsync();

            return lastActivity == default ? null : lastActivity;
        }

        public async Task<IEnumerable<ActivityLog>> GetUserActivitiesAsync(string userId, int take = 10)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetActivitiesByActionAsync(ActivityAction action, DateTime? from = null)
        {
            var query = Entities.AsNoTracking().Where(al => al.Action == action);

            if (from.HasValue)
                query = query.Where(al => al.Timestamp >= from.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetActivitiesByDateRangeAsync(DateTime from, DateTime to)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.Timestamp >= from && al.Timestamp <= to)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetFailedLoginAttemptsAsync(string userId, DateTime since)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.UserId == userId &&
                           al.Action == ActivityAction.Login &&
                           !al.Success &&
                           al.Timestamp >= since)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetFailedLoginAttemptsByIpAsync(string ipAddress, DateTime since)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.IpAddress == ipAddress &&
                           al.Action == ActivityAction.Login &&
                           !al.Success &&
                           al.Timestamp >= since)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 100)
        {
            return await Entities
                .AsNoTracking()
                .OrderByDescending(al => al.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Dictionary<ActivityAction, int>> GetActivityStatsByActionAsync(DateTime? from = null)
        {
            var query = Entities.AsNoTracking();

            if (from.HasValue)
                query = query.Where(al => al.Timestamp >= from.Value);

            return await query
                .GroupBy(al => al.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);
        }

        public async Task<IEnumerable<ActivityLog>> GetActivitiesByIpAddressAsync(string ipAddress, int take = 50)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.IpAddress == ipAddress)
                .OrderByDescending(al => al.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetSuspiciousActivitiesAsync(DateTime since)
        {
            // Подозрительная активность: неудачные попытки входа, изменения пароля, блокировки и т.д.
            var suspiciousActions = new[]
            {
                ActivityAction.Login,
                ActivityAction.ChangePassword,
                ActivityAction.ResetPassword,
                ActivityAction.BlockUser,
                ActivityAction.DeleteUser
            };

            return await Entities
                .AsNoTracking()
                .Where(al => al.Timestamp >= since &&
                           (suspiciousActions.Contains(al.Action) && !al.Success ||
                            al.Action == ActivityAction.BlockUser ||
                            al.Action == ActivityAction.DeleteUser))
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task CleanupOldActivitiesAsync(DateTime olderThan)
        {
            var oldLogs = await Entities
                .Where(al => al.Timestamp < olderThan)
                .ToListAsync();

            if (oldLogs.Any())
            {
                DeleteRange(oldLogs);
            }
        }

        public async Task<IEnumerable<ActivityLog>> GetActivitiesByEntityAsync(string entityType, string entityId)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.EntityType == entityType && al.EntityId == entityId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetUserActivityCountsAsync(DateTime from, DateTime to)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.Timestamp >= from && al.Timestamp <= to)
                .GroupBy(al => al.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);
        }

        public async Task<IEnumerable<(string UserId, int ActivityCount)>> GetTopActiveUsersAsync(DateTime from, DateTime to, int count = 10)
        {
            return await Entities
                .AsNoTracking()
                .Where(al => al.Timestamp >= from && al.Timestamp <= to)
                .GroupBy(al => al.UserId)
                .Select(g => new { UserId = g.Key, ActivityCount = g.Count() })
                .OrderByDescending(x => x.ActivityCount)
                .Take(count)
                .Select(x => ValueTuple.Create(x.UserId, x.ActivityCount))
                .ToListAsync();
        }
    }
}