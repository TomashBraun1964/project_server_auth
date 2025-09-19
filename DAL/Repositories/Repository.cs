using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DAL.Repositories.Interfaces.DAL.Repositories.Interfaces;

namespace DAL.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext Context;
        protected readonly DbSet<TEntity> Entities;

        public Repository(DbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Entities = context.Set<TEntity>();
        }

        // =============== СУЩЕСТВУЮЩИЕ МЕТОДЫ (без изменений) ===============
        public virtual async Task<TEntity?> GetByIdAsync(int id) => await Entities.FindAsync(id);

        public virtual async Task<TEntity?> GetByIdAsNoTrackingAsync(int id) =>
            await Entities.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync() =>
            await Entities.AsNoTracking().ToListAsync();

        public virtual IQueryable<TEntity> GetQueryable() => Entities.AsNoTracking();

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Entities.AsNoTracking().Where(predicate).ToListAsync();

        public virtual async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Entities.AsNoTracking().FirstOrDefaultAsync(predicate);

        public virtual async Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Entities.AsNoTracking().SingleOrDefaultAsync(predicate);

        public virtual async Task<(IEnumerable<TEntity> Items, int Total)> GetPagedAsync(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true,
            Expression<Func<TEntity, bool>>? filter = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            IQueryable<TEntity> query = Entities.AsNoTracking();

            if (filter != null) query = query.Where(filter);

            if (orderBy != null)
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            else
                query = query.OrderBy(e => EF.Property<int>(e, "Id"));

            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }

        public virtual IQueryable<TEntity> GetQueryableWithInclude(
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null)
        {
            IQueryable<TEntity> query = Entities.AsNoTracking();
            return include?.Invoke(query) ?? query;
        }

        public virtual async Task<bool> ExistsAsync(int id) =>
            await Entities.AnyAsync(e => EF.Property<int>(e, "Id") == id);

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Entities.AnyAsync(predicate);

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null) =>
            predicate == null ? await Entities.CountAsync() : await Entities.CountAsync(predicate);

        public virtual async Task AddAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await Entities.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            await Entities.AddRangeAsync(entities);
        }

        public virtual void Update(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Entities.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            Entities.UpdateRange(entities);
        }

        public virtual void Delete(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Entities.Remove(entity);
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            Entities.RemoveRange(entities);
        }

        public virtual async Task DeleteByIdAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null) Delete(entity);
        }

        // =============== НОВЫЕ МЕТОДЫ ===============

        // TRACKING ВЕРСИИ
        public virtual async Task<TEntity?> GetFirstOrDefaultTrackingAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Entities.FirstOrDefaultAsync(predicate);

        public virtual async Task<IEnumerable<TEntity>> FindTrackingAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Entities.Where(predicate).ToListAsync();

        // ЧАСТИЧНОЕ ОБНОВЛЕНИЕ
        public virtual async Task<bool> UpdatePartialAsync(int id, Expression<Func<TEntity, object>>[] propertiesToUpdate, object values)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            var entry = Context.Entry(entity);

            // Применяем значения из переданного объекта
            entry.CurrentValues.SetValues(values);

            // Помечаем только нужные свойства как измененные
            foreach (var property in propertiesToUpdate)
            {
                var propertyName = GetPropertyName(property);
                entry.Property(propertyName).IsModified = true;
            }

            return true;
        }

        public virtual async Task<int> UpdatePropertiesAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TEntity>> updateExpression)
        {
            // Для EF Core 7+ можно использовать ExecuteUpdateAsync
            // Для более старых версий нужно загрузить и обновить
            var entities = await Entities.Where(predicate).ToListAsync();

            foreach (var entity in entities)
            {
                var compiled = updateExpression.Compile();
                var updatedEntity = compiled(entity);
                Context.Entry(entity).CurrentValues.SetValues(updatedEntity);
            }

            return entities.Count;
        }

        // BULK ОПЕРАЦИИ
        public virtual async Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = await Entities.Where(predicate).ToListAsync();
            Entities.RemoveRange(entities);
            return entities.Count;
        }

        // ДОПОЛНИТЕЛЬНЫЕ ПРОВЕРКИ
        public virtual async Task<bool> IsUniqueAsync(Expression<Func<TEntity, object>> property, object value, int? excludeId = null)
        {
            var query = Entities.AsNoTracking().Where(BuildEqualityExpression(property, value));

            if (excludeId.HasValue)
                query = query.Where(e => EF.Property<int>(e, "Id") != excludeId.Value);

            return !await query.AnyAsync();
        }

        public virtual async Task<TResult?> GetMaxAsync<TResult>(Expression<Func<TEntity, TResult>> selector, Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = Entities.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);

            return await query.AnyAsync() ? await query.MaxAsync(selector) : default;
        }

        public virtual async Task<TResult?> GetMinAsync<TResult>(Expression<Func<TEntity, TResult>> selector, Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = Entities.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);

            return await query.AnyAsync() ? await query.MinAsync(selector) : default;
        }

        // SOFT DELETE ПОДДЕРЖКА
        public virtual async Task SoftDeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null && HasProperty(entity, "IsDeleted"))
            {
                Context.Entry(entity).Property("IsDeleted").CurrentValue = true;
                Context.Entry(entity).Property("DeletedAt").CurrentValue = DateTime.UtcNow;
            }
        }

        public virtual async Task<int> SoftDeleteManyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = await Entities.Where(predicate).ToListAsync();

            foreach (var entity in entities.Where(e => HasProperty(e, "IsDeleted")))
            {
                Context.Entry(entity).Property("IsDeleted").CurrentValue = true;
                Context.Entry(entity).Property("DeletedAt").CurrentValue = DateTime.UtcNow;
            }

            return entities.Count;
        }

        public virtual async Task RestoreAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null && HasProperty(entity, "IsDeleted"))
            {
                Context.Entry(entity).Property("IsDeleted").CurrentValue = false;
                Context.Entry(entity).Property("DeletedAt").CurrentValue = null;
            }
        }

        // ДОПОЛНИТЕЛЬНЫЕ ПОИСКИ
        public virtual async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids) =>
            await Entities.AsNoTracking().Where(e => ids.Contains(EF.Property<int>(e, "Id"))).ToListAsync();

        public virtual async Task<IEnumerable<TEntity>> GetRandomAsync(int count, Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = Entities.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);

            return await query.OrderBy(x => Guid.NewGuid()).Take(count).ToListAsync();
        }

        // ATTACH ОПЕРАЦИИ
        public virtual void Attach(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Entities.Attach(entity);
        }

        public virtual void Detach(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Context.Entry(entity).State = EntityState.Detached;
        }

        // =============== HELPER МЕТОДЫ ===============
        private static string GetPropertyName<T>(Expression<Func<T, object>> property)
        {
            if (property.Body is MemberExpression member)
                return member.Member.Name;
            if (property.Body is UnaryExpression unary && unary.Operand is MemberExpression memberOperand)
                return memberOperand.Member.Name;
            throw new ArgumentException("Invalid property expression");
        }

        private static Expression<Func<TEntity, bool>> BuildEqualityExpression(Expression<Func<TEntity, object>> property, object value)
        {
            var parameter = property.Parameters[0];
            var member = property.Body;
            var constant = Expression.Constant(value);
            var equality = Expression.Equal(member, constant);
            return Expression.Lambda<Func<TEntity, bool>>(equality, parameter);
        }

        private static bool HasProperty(object entity, string propertyName)
        {
            return entity.GetType().GetProperty(propertyName) != null;
        }
    }
}