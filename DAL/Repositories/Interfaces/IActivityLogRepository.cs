// DAL/Repositories/Interfaces/IActivityLogRepository.cs
using DAL.Models;
using DAL.Repositories.Interfaces.DAL.Repositories.Interfaces;

namespace DAL.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с логами активности
    /// </summary>
    public interface IActivityLogRepository : IRepository<ActivityLog>
    {
        /// <summary>
        /// Получение последней активности пользователя
        /// </summary>
        Task<DateTime?> GetLastActivityDateAsync(string userId);

        /// <summary>
        /// Получение активности пользователя с пагинацией
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetUserActivitiesAsync(string userId, int take = 10);

        /// <summary>
        /// Получение активности по типу действия
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetActivitiesByActionAsync(ActivityAction action, DateTime? from = null);

        /// <summary>
        /// Получение активности за период
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetActivitiesByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>
        /// Получение неудачных попыток входа для пользователя
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetFailedLoginAttemptsAsync(string userId, DateTime since);

        /// <summary>
        /// Получение неудачных попыток входа по IP
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetFailedLoginAttemptsByIpAsync(string ipAddress, DateTime since);

        /// <summary>
        /// Получение последних логов активности
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 100);

        /// <summary>
        /// Получение статистики активности по действиям
        /// </summary>
        Task<Dictionary<ActivityAction, int>> GetActivityStatsByActionAsync(DateTime? from = null);

        /// <summary>
        /// Получение активности по IP адресу
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetActivitiesByIpAddressAsync(string ipAddress, int take = 50);

        /// <summary>
        /// Поиск подозрительной активности
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetSuspiciousActivitiesAsync(DateTime since);

        /// <summary>
        /// Очистка старых логов активности
        /// </summary>
        Task CleanupOldActivitiesAsync(DateTime olderThan);

        /// <summary>
        /// Получение активности с деталями по сущности
        /// </summary>
        Task<IEnumerable<ActivityLog>> GetActivitiesByEntityAsync(string entityType, string entityId);

        /// <summary>
        /// Подсчет активности пользователей за период
        /// </summary>
        Task<Dictionary<string, int>> GetUserActivityCountsAsync(DateTime from, DateTime to);

        /// <summary>
        /// Получение топ активных пользователей
        /// </summary>
        Task<IEnumerable<(string UserId, int ActivityCount)>> GetTopActiveUsersAsync(DateTime from, DateTime to, int count = 10);
    }
}