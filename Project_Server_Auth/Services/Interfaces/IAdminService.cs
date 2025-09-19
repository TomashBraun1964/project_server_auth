// Services/Interfaces/ISimpleAdminService.cs
using DAL.Models;
using Project_Server_Auth.Dtos;

namespace Project_Server_Auth.Services.Interfaces
{
    public interface ISimpleAdminService
    {
        // Управление пользователями - базовые операции
        Task<PagedResponseDto<SimpleAdminUserDto>> GetUsersAsync(BasePagedRequest request);
        Task<SimpleAdminUserDto?> GetUserByIdAsync(string userId);
        Task<bool> CreateUserAsync(SimpleAdminCreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(string userId, SimpleAdminUpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ActivateUserAsync(string userId);
        Task<bool> DeactivateUserAsync(string userId);
        Task<bool> ChangeUserPasswordAsync(string userId, string newPassword);

        // Управление сессиями - базовые операции
        Task<PagedResponseDto<SimpleUserSessionDto>> GetAllSessionsAsync(BasePagedRequest request);
        Task<List<SimpleUserSessionDto>> GetUserSessionsAsync(string userId);
        Task<bool> RevokeSessionAsync(int sessionId);
        Task<bool> RevokeAllUserSessionsAsync(string userId);

        // Логи активности - просмотр
        Task<PagedResponseDto<SimpleActivityLogDto>> GetActivityLogsAsync(BasePagedRequest request);
        Task<List<SimpleActivityLogDto>> GetUserActivityLogsAsync(string userId, int limit = 50);

        // Простая статистика
        Task<SimpleStatsDto> GetSimpleStatisticsAsync();
    }
}