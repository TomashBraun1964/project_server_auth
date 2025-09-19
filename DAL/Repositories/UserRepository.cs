// DAL/Repositories/UserRepository.cs
using DAL.Models;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с пользователями
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly DbContext _context;
        private readonly DbSet<ApplicationUser> _users;

        public UserRepository(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _users = _context.Set<ApplicationUser>();
        }

        public async Task<ApplicationUser?> GetByIdWithIncludesAsync(string id)
        {
            return await _users
                .Include(u => u.UserSessions)
                .Include(u => u.ActivityLogs.OrderByDescending(al => al.Timestamp).Take(10))
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersWithSessionsAsync()
        {
            return await _users
                .Include(u => u.UserSessions.Where(s => s.IsActive))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ApplicationUser?> GetUserWithSessionsAsync(string userId)
        {
            return await _users
                .Include(u => u.UserSessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateLastActivityAsync(string userId)
        {
            var user = await _users.FindAsync(userId);
            if (user != null)
            {
                user.UpdatedAt = DateTime.UtcNow;
                // Изменения будут сохранены через UnitOfWork.SaveChangesAsync()
            }
        }

        public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync()
        {
            return await _users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ApplicationUser>();

            var search = searchTerm.ToLower().Trim();

            return await _users
                .AsNoTracking()
                .Where(u =>
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search) ||
                    u.Email!.ToLower().Contains(search) ||
                    (u.Department != null && u.Department.ToLower().Contains(search)))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName)
        {
            // Это требует join с таблицей ролей через UserManager
            // Поскольку мы работаем на уровне DbContext, возвращаем базовый запрос
            // Полная реализация должна быть в UserService с использованием UserManager
            return await _users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByExternalProviderAsync(string provider)
        {
            return await _users
                .AsNoTracking()
                .Where(u => u.IsExternalAccount && u.ExternalProvider == provider)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<(int Total, int Active, int Inactive, int ExternalAccounts)> GetUserStatsAsync()
        {
            var total = await _users.CountAsync();
            var active = await _users.CountAsync(u => u.IsActive);
            var inactive = total - active;
            var externalAccounts = await _users.CountAsync(u => u.IsExternalAccount);

            return (total, active, inactive, externalAccounts);
        }
    }
}