// Services/UserService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<UserProfileDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null ? MapToUserProfileDto(user) : null;
        }

        public async Task<UserProfileDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null ? MapToUserProfileDto(user) : null;
        }

        public async Task<bool> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                // Обновляем только переданные поля
                if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                    user.FirstName = updateUserDto.FirstName;

                if (!string.IsNullOrEmpty(updateUserDto.LastName))
                    user.LastName = updateUserDto.LastName;

                if (updateUserDto.Avatar != null)
                    user.Avatar = updateUserDto.Avatar;

                if (updateUserDto.Department != null)
                    user.Department = updateUserDto.Department;

                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при деактивации пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ActivateUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при активации пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<PagedResponseDto<UserListItemDto>> GetUsersAsync(UserFilterDto filter)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Применяем фильтры
                if (filter.IsActive.HasValue)
                    query = query.Where(u => u.IsActive == filter.IsActive.Value);

                if (!string.IsNullOrEmpty(filter.Department))
                    query = query.Where(u => u.Department == filter.Department);

                if (filter.IsExternalAccount.HasValue)
                    query = query.Where(u => u.IsExternalAccount == filter.IsExternalAccount.Value);

                if (!string.IsNullOrEmpty(filter.ExternalProvider))
                    query = query.Where(u => u.ExternalProvider == filter.ExternalProvider);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(u =>
                        u.FirstName.Contains(filter.SearchTerm) ||
                        u.LastName.Contains(filter.SearchTerm) ||
                        u.Email.Contains(filter.SearchTerm));
                }

                // Фильтр по датам
                if (filter.StartDate.HasValue)
                    query = query.Where(u => u.CreatedAt >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(u => u.CreatedAt <= filter.EndDate.Value);

                // Сортировка
                if (!string.IsNullOrEmpty(filter.SortBy))
                {
                    switch (filter.SortBy.ToLower())
                    {
                        case "email":
                            query = filter.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email);
                            break;
                        case "firstname":
                            query = filter.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName);
                            break;
                        case "lastname":
                            query = filter.SortDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName);
                            break;
                        case "lastlogin":
                            query = filter.SortDescending ? query.OrderByDescending(u => u.LastLogin) : query.OrderBy(u => u.LastLogin);
                            break;
                        default:
                            query = filter.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(u => u.CreatedAt);
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(u => new UserListItemDto
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        Email = u.Email ?? "",
                        Department = u.Department,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLogin = u.LastLogin,
                        IsExternalAccount = u.IsExternalAccount,
                        ExternalProvider = u.ExternalProvider
                    })
                    .ToListAsync();

                return new PagedResponseDto<UserListItemDto>
                {
                    Data = users,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return new PagedResponseDto<UserListItemDto>();
            }
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);

                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var inactiveUsers = totalUsers - activeUsers;
                var externalAccounts = await _context.Users.CountAsync(u => u.IsExternalAccount);
                var usersWithTwoFactor = await _context.Users.CountAsync(u => u.TwoFactorEnabled);

                var usersByDepartment = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.Department))
                    .GroupBy(u => u.Department)
                    .Select(g => new { Department = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Department!, x => x.Count);

                var registrationTrend = await _context.Users
                    .Where(u => u.CreatedAt >= thirtyDaysAgo)
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new DailyRegistrationDto
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return new UserStatisticsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    ExternalAccounts = externalAccounts,
                    UsersWithTwoFactor = usersWithTwoFactor,
                    UsersByDepartment = usersByDepartment,
                    RegistrationTrend = registrationTrend
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики пользователей");
                return new UserStatisticsDto();
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 10)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive &&
                        (u.FirstName.Contains(query) ||
                         u.LastName.Contains(query) ||
                         u.Email.Contains(query)))
                    .Take(limit)
                    .Select(u => new UserSearchResultDto
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        Email = u.Email ?? "",
                        Department = u.Department,
                        Avatar = u.Avatar,
                        IsActive = u.IsActive
                    })
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске пользователей");
                return new List<UserSearchResultDto>();
            }
        }

        private UserProfileDto MapToUserProfileDto(ApplicationUser user)
        {
            return new UserProfileDto
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                Department = user.Department,
                Avatar = user.Avatar,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }
    }
}