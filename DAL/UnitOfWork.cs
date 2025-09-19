// DAL/UnitOfWork.cs - ПОЛНАЯ РЕАЛИЗАЦИЯ
using DAL.Interfaces;
using DAL.Repositories;
using DAL.Repositories.Interfaces;
using DAL.Repositories.Interfaces.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace DAL
{
    /// <summary>
    /// Полная реализация Unit of Work паттерна
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private bool _disposed = false;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ===========================
        // СВОЙСТВА КОНТЕКСТА
        // ===========================
        public DbContext Context => _context;

        // ===========================
        // СПЕЦИАЛИЗИРОВАННЫЕ РЕПОЗИТОРИИ с lazy loading
        // ===========================
        private IUserRepository? _userRepository;
        public IUserRepository Users => _userRepository ??= new UserRepository(_context);

        private IUserSessionRepository? _userSessionRepository;
        public IUserSessionRepository UserSessions => _userSessionRepository ??= new UserSessionRepository(_context);

        private IActivityLogRepository? _activityLogRepository;
        public IActivityLogRepository ActivityLogs => _activityLogRepository ??= new ActivityLogRepository(_context);

        // ===========================
        // GENERIC REPOSITORY
        // ===========================
        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            if (_repositories.TryGetValue(typeof(TEntity), out var repository))
                return (IRepository<TEntity>)repository;

            var newRepo = new Repository<TEntity>(_context);
            _repositories.Add(typeof(TEntity), newRepo);
            return newRepo;
        }

        // ===========================
        // СОХРАНЕНИЕ ИЗМЕНЕНИЙ
        // ===========================
        public int SaveChanges()
        {
            try
            {
                return _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("Конфликт одновременных обновлений при сохранении изменений.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Ошибка при сохранении изменений в базе данных.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Неожиданная ошибка при сохранении изменений.", ex);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("Конфликт одновременных обновлений при сохранении изменений.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Ошибка при сохранении изменений в базе данных.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Неожиданная ошибка при сохранении изменений.", ex);
            }
        }

        // ===========================
        // ТРАНЗАКЦИИ - УПРОЩЕННЫЕ МЕТОДЫ
        // ===========================
        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
                throw new InvalidOperationException("Транзакция уже начата. Завершите текущую транзакцию перед началом новой.");

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("Нет активной транзакции для подтверждения.");

            try
            {
                await _currentTransaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await RollbackTransactionAsync();
                throw new InvalidOperationException("Ошибка при подтверждении транзакции.", ex);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("Нет активной транзакции для отката.");

            try
            {
                await _currentTransaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при откате транзакции.", ex);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        // ===========================
        // ТРАНЗАКЦИИ - РАСШИРЕННЫЕ МЕТОДЫ
        // ===========================
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Database.BeginTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при начале транзакции.", ex);
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // ===========================
        // SQL КОМАНДЫ
        // ===========================
        public async Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL запрос не может быть пустым.", nameof(sql));

            try
            {
                return await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при выполнении SQL запроса: {sql}", ex);
            }
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL запрос не может быть пустым.", nameof(sql));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            try
            {
                return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при выполнении SQL запроса с параметрами: {sql}", ex);
            }
        }

        // ===========================
        // УПРАВЛЕНИЕ КОНТЕКСТОМ
        // ===========================
        public void DetachAllEntities()
        {
            try
            {
                _context.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при отсоединении сущностей от контекста.", ex);
            }
        }

        public async Task ResetSequenceAsync<TEntity>() where TEntity : class
        {
            try
            {
                var entityType = _context.Model.FindEntityType(typeof(TEntity));
                if (entityType == null)
                    throw new InvalidOperationException($"Тип сущности {typeof(TEntity).Name} не найден в модели.");

                var tableName = entityType.GetTableName();
                var schemaName = entityType.GetSchema() ?? "public";

                if (string.IsNullOrEmpty(tableName))
                    throw new InvalidOperationException($"Не удалось получить имя таблицы для типа {typeof(TEntity).Name}.");

                // PostgreSQL версия сброса последовательности
                var sequenceName = $"{tableName}_id_seq";
                var sql = $"ALTER SEQUENCE {schemaName}.{sequenceName} RESTART WITH 1";

                await ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при сбросе последовательности для {typeof(TEntity).Name}.", ex);
            }
        }

        public bool HasUnsavedChanges()
        {
            try
            {
                return _context.ChangeTracker.HasChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при проверке несохраненных изменений.", ex);
            }
        }

        public void RejectChanges()
        {
            try
            {
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.CurrentValues.SetValues(entry.OriginalValues);
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.State = EntityState.Unchanged;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при отмене изменений.", ex);
            }
        }

        // ===========================
        // ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ
        // ===========================
        public async Task<bool> CanConnectAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetConnectionState()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                return connection.State.ToString();
            }
            catch (Exception ex)
            {
                return $"Ошибка получения состояния: {ex.Message}";
            }
        }

        public async Task MigrateAsync()
        {
            try
            {
                await _context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при выполнении миграций.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetPendingMigrationsAsync()
        {
            try
            {
                return await _context.Database.GetPendingMigrationsAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при получении списка ожидающих миграций.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
        {
            try
            {
                return await _context.Database.GetAppliedMigrationsAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при получении списка примененных миграций.", ex);
            }
        }

        // ===========================
        // DISPOSABLE PATTERN
        // ===========================
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // Откатываем активную транзакцию при освобождении ресурсов
                    if (_currentTransaction != null)
                    {
                        _currentTransaction.Rollback();
                        _currentTransaction.Dispose();
                        _currentTransaction = null;
                    }

                    // Очищаем кэш репозиториев
                    _repositories.Clear();

                    // Освобождаем контекст
                    _context?.Dispose();
                }
                catch (Exception)
                {
                    // Игнорируем ошибки при освобождении ресурсов
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}