// Services/Interfaces/IActivityLogService.cs
using Project_Server_Auth.Dtos;
using DAL.Models;

namespace Project_Server_Auth.Services.Interfaces
{
    public interface IActivityLogService
    {
        // Основное логирование
        Task LogActivityAsync(string userId, ActivityAction action, bool success = true, string? details = null, string? entityType = null, string? entityId = null, string? ipAddress = null, string? userAgent = null);

        // Получение логов с фильтрацией и пагинацией
        Task<PagedResponseDto<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter);

        // Получение логов конкретного пользователя
        Task<List<ActivityLogDto>> GetUserActivityLogsAsync(string userId, int limit = 50);

        // Получение конкретного лога по ID
        Task<ActivityLogDto?> GetActivityLogByIdAsync(int id);

        // Очистка старых логов (для поддержания производительности)
        Task<int> CleanupOldLogsAsync(int daysToKeep = 90);

        // Получение последних активностей (для дашборда)
        Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int limit = 20);

        // Статистика по действиям за период
        Task<Dictionary<ActivityAction, int>> GetActivityStatisticsAsync(DateTime? from = null, DateTime? to = null);
    }
}