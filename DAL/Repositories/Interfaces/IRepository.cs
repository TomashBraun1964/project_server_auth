using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace DAL.Repositories.Interfaces
{
    // =============================================================================
    // IRepository.cs - ДОПОЛНЕННЫЙ интерфейс с важными методами
    // =============================================================================
    using Microsoft.EntityFrameworkCore.Query;
    using System.Linq.Expressions;

    namespace DAL.Repositories.Interfaces
    {
        public interface IRepository<TEntity> where TEntity : class
        {
            // ===========================
            // СУЩЕСТВУЮЩИЕ МЕТОДЫ (без изменений)
            // ===========================
            Task<TEntity?> GetByIdAsync(int id);
            Task<TEntity?> GetByIdAsNoTrackingAsync(int id);
            Task<IEnumerable<TEntity>> GetAllAsync();
            IQueryable<TEntity> GetQueryable();
            Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
            Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
            Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

            Task<(IEnumerable<TEntity> Items, int Total)> GetPagedAsync(
                int pageNumber, int pageSize,
                Expression<Func<TEntity, object>>? orderBy = null,
                bool ascending = true,
                Expression<Func<TEntity, bool>>? filter = null);

            IQueryable<TEntity> GetQueryableWithInclude(
                Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null);

            Task<bool> ExistsAsync(int id);
            Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
            Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

            Task AddAsync(TEntity entity);
            Task AddRangeAsync(IEnumerable<TEntity> entities);
            void Update(TEntity entity);
            void UpdateRange(IEnumerable<TEntity> entities);
            void Delete(TEntity entity);
            void DeleteRange(IEnumerable<TEntity> entities);
            Task DeleteByIdAsync(int id);

            // ===========================
            // НОВЫЕ ВАЖНЫЕ МЕТОДЫ
            // ===========================

            // TRACKING ВЕРСИИ для редактирования
            /// <summary>
            /// Получает сущность по условию С отслеживанием изменений (для редактирования)
            /// </summary>
            Task<TEntity?> GetFirstOrDefaultTrackingAsync(Expression<Func<TEntity, bool>> predicate);

            /// <summary>
            /// Получает все сущности по условию С отслеживанием изменений
            /// </summary>
            Task<IEnumerable<TEntity>> FindTrackingAsync(Expression<Func<TEntity, bool>> predicate);

            // ЧАСТИЧНОЕ ОБНОВЛЕНИЕ
            /// <summary>
            /// Частичное обновление сущности - обновляет только указанные свойства
            /// </summary>
            Task<bool> UpdatePartialAsync(int id, Expression<Func<TEntity, object>>[] propertiesToUpdate,
                object values);

            /// <summary>
            /// Обновление конкретных свойств сущности по условию
            /// </summary>
            Task<int> UpdatePropertiesAsync(
                Expression<Func<TEntity, bool>> predicate,
                Expression<Func<TEntity, TEntity>> updateExpression);

            // BULK ОПЕРАЦИИ
            /// <summary>
            /// Массовое удаление по условию (без загрузки в память)
            /// </summary>
            Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> predicate);

            // ДОПОЛНИТЕЛЬНЫЕ ПРОВЕРКИ
            /// <summary>
            /// Проверяет уникальность значения поля
            /// </summary>
            Task<bool> IsUniqueAsync(Expression<Func<TEntity, object>> property, object value, int? excludeId = null);

            /// <summary>
            /// Получает максимальное значение для указанного поля
            /// </summary>
            Task<TResult?> GetMaxAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
                Expression<Func<TEntity, bool>>? predicate = null);

            /// <summary>
            /// Получает минимальное значение для указанного поля
            /// </summary>
            Task<TResult?> GetMinAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
                Expression<Func<TEntity, bool>>? predicate = null);

            // SOFT DELETE ПОДДЕРЖКА
            /// <summary>
            /// Мягкое удаление (устанавливает IsDeleted = true вместо физического удаления)
            /// </summary>
            Task SoftDeleteAsync(int id);

            /// <summary>
            /// Мягкое удаление по условию
            /// </summary>
            Task<int> SoftDeleteManyAsync(Expression<Func<TEntity, bool>> predicate);

            /// <summary>
            /// Восстановление мягко удаленной сущности
            /// </summary>
            Task RestoreAsync(int id);

            // ДОПОЛНИТЕЛЬНЫЕ ПОИСКИ
            /// <summary>
            /// Поиск по нескольким ID за один запрос
            /// </summary>
            Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids);

            /// <summary>
            /// Получение случайных записей
            /// </summary>
            Task<IEnumerable<TEntity>> GetRandomAsync(int count, Expression<Func<TEntity, bool>>? predicate = null);

            // ATTACH ОПЕРАЦИИ
            /// <summary>
            /// Присоединяет сущность к контексту для отслеживания изменений
            /// </summary>
            void Attach(TEntity entity);

            /// <summary>
            /// Отсоединяет сущность от контекста
            /// </summary>
            void Detach(TEntity entity);
        }
    }
}