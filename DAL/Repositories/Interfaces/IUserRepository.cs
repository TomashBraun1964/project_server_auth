// DAL/Repositories/Interfaces/IUserRepository.cs
using DAL.Models;

namespace DAL.Repositories.Interfaces
{
    /// <summary>
    /// Специализированный репозиторий для работы с пользователями
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Получение пользователя по ID с подключением связанных данных
        /// </summary>
        Task<ApplicationUser?> GetByIdWithIncludesAsync(string id);

        /// <summary>
        /// Получение пользователя по email
        /// </summary>
        Task<ApplicationUser?> GetByEmailAsync(string email);

        /// <summary>
        /// Проверка существования email
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>
        /// Получение пользователей с их сессиями
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetUsersWithSessionsAsync();

        /// <summary>
        /// Получение пользователя с его сессиями
        /// </summary>
        Task<ApplicationUser?> GetUserWithSessionsAsync(string userId);

        /// <summary>
        /// Обновление времени последней активности пользователя
        /// </summary>
        Task UpdateLastActivityAsync(string userId);

        /// <summary>
        /// Получение активных пользователей
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync();

        /// <summary>
        /// Поиск пользователей по тексту (имя, фамилия, email, отдел)
        /// </summary>
        Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string searchTerm);

        /// <summary>
        /// Получение пользователей по роли
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName);

        /// <summary>
        /// Получение пользователей по внешнему провайдеру
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetUsersByExternalProviderAsync(string provider);

        /// <summary>
        /// Получение статистики пользователей
        /// </summary>
        Task<(int Total, int Active, int Inactive, int ExternalAccounts)> GetUserStatsAsync();
    }
}