// DAL/Repositories/Interfaces/IUserSessionRepository.cs
using DAL.Models;
using DAL.Repositories.Interfaces.DAL.Repositories.Interfaces;

namespace DAL.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с пользовательскими сессиями
    /// </summary>
    public interface IUserSessionRepository : IRepository<UserSession>
    {
        /// <summary>
        /// Получение активных сессий пользователя
        /// </summary>
        Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(string userId);

        /// <summary>
        /// Получение всех сессий пользователя (включая неактивные)
        /// </summary>
        Task<IEnumerable<UserSession>> GetAllSessionsByUserIdAsync(string userId);

        /// <summary>
        /// Получение сессии по refresh token
        /// </summary>
        Task<UserSession?> GetByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Отзыв всех активных сессий пользователя
        /// </summary>
        Task RevokeAllUserSessionsAsync(string userId);

        /// <summary>
        /// Отзыв конкретной сессии
        /// </summary>
        Task RevokeSessionAsync(int sessionId);

        /// <summary>
        /// Получение количества активных сессий пользователя
        /// </summary>
        Task<int> GetActiveSessionsCountAsync(string userId);

        /// <summary>
        /// Отзыв всех сессий кроме текущей
        /// </summary>
        Task RevokeOtherSessionsAsync(string userId, string currentRefreshToken);

        /// <summary>
        /// Получение просроченных сессий
        /// </summary>
        Task<IEnumerable<UserSession>> GetExpiredSessionsAsync();

        /// <summary>
        /// Очистка просроченных сессий
        /// </summary>
        Task CleanupExpiredSessionsAsync();

        /// <summary>
        /// Получение сессий по IP адресу
        /// </summary>
        Task<IEnumerable<UserSession>> GetSessionsByIpAddressAsync(string ipAddress);

        /// <summary>
        /// Получение последних активных сессий (для мониторинга)
        /// </summary>
        Task<IEnumerable<UserSession>> GetRecentActiveSessionsAsync(int count = 100);

        /// <summary>
        /// Проверка достижения лимита сессий для пользователя
        /// </summary>
        Task<bool> IsSessionLimitReachedAsync(string userId, int maxSessions);
    }
}