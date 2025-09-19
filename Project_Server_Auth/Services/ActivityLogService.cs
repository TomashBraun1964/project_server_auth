// Services/ActivityLogService.cs
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(AppDbContext context, ILogger<ActivityLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActivityAsync(string userId, ActivityAction action, bool success = true, string? details = null, string? entityType = null, string? entityId = null, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    UserId = userId,
                    Action = action,
                    Success = success,
                    Details = details,
                    EntityType = entityType,
                    EntityId = entityId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceType = ExtractDeviceType(userAgent),
                    Timestamp = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при логировании активности пользователя {UserId}, действие {Action}", userId, action);
            }
        }

        public async Task<PagedResponseDto<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter)
        {
            try
            {
                var query = _context.ActivityLogs
                    .Include(a => a.User)
                    .AsQueryable();

                // Применяем фильтры
                if (!string.IsNullOrEmpty(filter.UserId))
                    query = query.Where(a => a.UserId == filter.UserId);

                if (filter.Action.HasValue)
                    query = query.Where(a => a.Action == filter.Action.Value);

                if (filter.Success.HasValue)
                    query = query.Where(a => a.Success == filter.Success.Value);

                if (filter.DeviceType.HasValue)
                    query = query.Where(a => a.DeviceType == filter.DeviceType.Value);

                if (!string.IsNullOrEmpty(filter.EntityType))
                    query = query.Where(a => a.EntityType == filter.EntityType);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(a =>
                        a.Details != null && a.Details.Contains(filter.SearchTerm) ||
                        a.User.FirstName.Contains(filter.SearchTerm) ||
                        a.User.LastName.Contains(filter.SearchTerm) ||
                        a.User.Email != null && a.User.Email.Contains(filter.SearchTerm));
                }

                if (filter.StartDate.HasValue)
                    query = query.Where(a => a.Timestamp >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(a => a.Timestamp <= filter.EndDate.Value);

                // Сортировка
                query = query.OrderByDescending(a => a.Timestamp);

                var totalCount = await query.CountAsync();

                var logs = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        UserFullName = a.User.FirstName + " " + a.User.LastName,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        Details = a.Details,
                        Success = a.Success,
                        Timestamp = a.Timestamp,
                        DeviceType = a.DeviceType,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent
                    })
                    .ToListAsync();

                return new PagedResponseDto<ActivityLogDto>
                {
                    Data = logs,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности");
                return new PagedResponseDto<ActivityLogDto>();
            }
        }

        public async Task<List<ActivityLogDto>> GetUserActivityLogsAsync(string userId, int limit = 50)
        {
            try
            {
                var logs = await _context.ActivityLogs
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        UserFullName = a.User.FirstName + " " + a.User.LastName,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        Details = a.Details,
                        Success = a.Success,
                        Timestamp = a.Timestamp,
                        DeviceType = a.DeviceType,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent
                    })
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности пользователя {UserId}", userId);
                return new List<ActivityLogDto>();
            }
        }

        public async Task<ActivityLogDto?> GetActivityLogByIdAsync(int id)
        {
            try
            {
                var log = await _context.ActivityLogs
                    .Include(a => a.User)
                    .Where(a => a.Id == id)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        UserFullName = a.User.FirstName + " " + a.User.LastName,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        Details = a.Details,
                        Success = a.Success,
                        Timestamp = a.Timestamp,
                        DeviceType = a.DeviceType,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent
                    })
                    .FirstOrDefaultAsync();

                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении лога активности {Id}", id);
                return null;
            }
        }

        public async Task<int> CleanupOldLogsAsync(int daysToKeep = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldLogs = await _context.ActivityLogs
                    .Where(a => a.Timestamp < cutoffDate)
                    .ToListAsync();

                _context.ActivityLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                return oldLogs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке старых логов");
                return 0;
            }
        }

        public async Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int limit = 20)
        {
            try
            {
                var logs = await _context.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        UserFullName = a.User.FirstName + " " + a.User.LastName,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        Details = a.Details,
                        Success = a.Success,
                        Timestamp = a.Timestamp,
                        DeviceType = a.DeviceType,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent
                    })
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении последних активностей");
                return new List<ActivityLogDto>();
            }
        }

        public async Task<Dictionary<ActivityAction, int>> GetActivityStatisticsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.ActivityLogs.AsQueryable();

                if (from.HasValue)
                    query = query.Where(a => a.Timestamp >= from.Value);

                if (to.HasValue)
                    query = query.Where(a => a.Timestamp <= to.Value);

                var statistics = await query
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Action, x => x.Count);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики активности");
                return new Dictionary<ActivityAction, int>();
            }
        }

        private DeviceType ExtractDeviceType(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return DeviceType.Unknown;

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
                return DeviceType.Mobile;
            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return DeviceType.Tablet;

            return DeviceType.Desktop;
        }
    }
}