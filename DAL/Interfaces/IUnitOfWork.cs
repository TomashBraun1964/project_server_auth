// DAL/Interfaces/IUnitOfWork.cs
using DAL.Repositories.Interfaces;
using DAL.Repositories.Interfaces.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.Interfaces
{
    /// <summary>
    /// Unit of Work паттерн для управления транзакциями и репозиториями
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // ===========================
        // СВОЙСТВА КОНТЕКСТА
        // ===========================
        /// <summary>
        /// Доступ к DbContext
        /// </summary>
        DbContext Context { get; }

        // ===========================
        // СПЕЦИАЛИЗИРОВАННЫЕ РЕПОЗИТОРИИ
        // ===========================
        /// <summary>
        /// Репозиторий для работы с пользователями
        /// </summary>
        IUserRepository Users { get; }

        /// <summary>
        /// Репозиторий для работы с сессиями пользователей
        /// </summary>
        IUserSessionRepository UserSessions { get; }

        /// <summary>
        /// Репозиторий для работы с логами активности
        /// </summary>
        IActivityLogRepository ActivityLogs { get; }

        // ===========================
        // GENERIC REPOSITORY
        // ===========================
        /// <summary>
        /// Получает generic репозиторий для любой сущности
        /// </summary>
        /// <typeparam name="TEntity">Тип сущности</typeparam>
        /// <returns>Репозиторий для работы с сущностью</returns>
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        // ===========================
        // СОХРАНЕНИЕ ИЗМЕНЕНИЙ
        // ===========================
        /// <summary>
        /// Синхронное сохранение изменений
        /// </summary>
        /// <returns>Количество затронутых записей</returns>
        int SaveChanges();

        /// <summary>
        /// Асинхронное сохранение изменений
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Количество затронутых записей</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // ===========================
        // ТРАНЗАКЦИИ - УПРОЩЕННЫЕ МЕТОДЫ
        // ===========================
        /// <summary>
        /// Начинает новую транзакцию (упрощенный метод)
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Подтверждает текущую транзакцию (упрощенный метод)
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Откатывает текущую транзакцию (упрощенный метод)
        /// </summary>
        Task RollbackTransactionAsync();

        // ===========================
        // ТРАНЗАКЦИИ - РАСШИРЕННЫЕ МЕТОДЫ
        // ===========================
        /// <summary>
        /// Начинает транзакцию и возвращает объект транзакции
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Объект транзакции</returns>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Выполняет действие в транзакции с автоматическим commit/rollback
        /// </summary>
        /// <param name="action">Действие для выполнения</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Выполняет действие в транзакции с возвращением результата
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого результата</typeparam>
        /// <param name="action">Действие для выполнения</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат выполнения действия</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

        // ===========================
        // SQL КОМАНДЫ
        // ===========================
        /// <summary>
        /// Выполняет сырой SQL запрос
        /// </summary>
        /// <param name="sql">SQL запрос</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Количество затронутых записей</returns>
        Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default);

        /// <summary>
        /// Выполняет SQL запрос с параметрами
        /// </summary>
        /// <param name="sql">SQL запрос</param>
        /// <param name="parameters">Параметры запроса</param>
        /// <returns>Количество затронутых записей</returns>
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);

        // ===========================
        // УПРАВЛЕНИЕ КОНТЕКСТОМ
        // ===========================
        /// <summary>
        /// Отсоединяет все сущности от контекста
        /// </summary>
        void DetachAllEntities();

        /// <summary>
        /// Сбрасывает автоинкремент для таблицы (PostgreSQL версия)
        /// </summary>
        /// <typeparam name="TEntity">Тип сущности</typeparam>
        Task ResetSequenceAsync<TEntity>() where TEntity : class;

        /// <summary>
        /// Проверяет, есть ли несохраненные изменения
        /// </summary>
        /// <returns>True, если есть несохраненные изменения</returns>
        bool HasUnsavedChanges();

        /// <summary>
        /// Отменяет все изменения в контексте
        /// </summary>
        void RejectChanges();

        // ===========================
        // ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ
        // ===========================
        /// <summary>
        /// Проверяет доступность базы данных
        /// </summary>
        /// <returns>True, если база данных доступна</returns>
        Task<bool> CanConnectAsync();

        /// <summary>
        /// Получает информацию о состоянии подключения
        /// </summary>
        /// <returns>Состояние подключения</returns>
        string GetConnectionState();

        /// <summary>
        /// Выполняет миграции базы данных
        /// </summary>
        Task MigrateAsync();

        /// <summary>
        /// Получает список ожидающих миграций
        /// </summary>
        /// <returns>Список миграций</returns>
        Task<IEnumerable<string>> GetPendingMigrationsAsync();

        /// <summary>
        /// Получает список примененных миграций
        /// </summary>
        /// <returns>Список миграций</returns>
        Task<IEnumerable<string>> GetAppliedMigrationsAsync();
    }
}