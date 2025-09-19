// Services/SimpleAdminService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Services
{
    public class SimpleAdminService : ISimpleAdminService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SimpleAdminService> _logger;

        public SimpleAdminService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SimpleAdminService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        #region Управление пользователями

        public async Task<PagedResponseDto<SimpleAdminUserDto>> GetUsersAsync(BasePagedRequest request)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Поиск по имени, фамилии или email
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(u =>
                        u.FirstName.Contains(request.SearchTerm) ||
                        u.LastName.Contains(request.SearchTerm) ||
                        u.Email.Contains(request.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(u => new SimpleAdminUserDto
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        Email = u.Email ?? "",
                        Department = u.Department,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLogin = u.LastLogin,
                        ActiveSessionsCount = u.UserSessions.Count(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                    })
                    .ToListAsync();

                return new PagedResponseDto<SimpleAdminUserDto>
                {
                    Data = users,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return new PagedResponseDto<SimpleAdminUserDto>();
            }
        }

        public async Task<SimpleAdminUserDto?> GetUserByIdAsync(string userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new SimpleAdminUserDto
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        Email = u.Email ?? "",
                        Department = u.Department,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLogin = u.LastLogin,
                        ActiveSessionsCount = u.UserSessions.Count(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                    })
                    .FirstOrDefaultAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(SimpleAdminCreateUserDto createUserDto)
        {
            try
            {
                var user = new ApplicationUser
                {
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    Email = createUserDto.Email,
                    UserName = createUserDto.Email,
                    Department = createUserDto.Department,
                    IsActive = createUserDto.IsActive,
                    EmailConfirmed = true // Админ создает подтвержденные аккаунты
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании пользователя");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(string userId, SimpleAdminUpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                    user.FirstName = updateUserDto.FirstName;

                if (!string.IsNullOrEmpty(updateUserDto.LastName))
                    user.LastName = updateUserDto.LastName;

                if (updateUserDto.Department != null)
                    user.Department = updateUserDto.Department;

                if (updateUserDto.IsActive.HasValue)
                    user.IsActive = updateUserDto.IsActive.Value;

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

        public async Task<bool> DeactivateUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                // Отзываем все активные сессии при деактивации
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = DateTime.UtcNow;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при деактивации пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangeUserPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                if (result.Succeeded)
                {
                    // Отзываем все сессии для безопасности
                    await RevokeAllUserSessionsAsync(userId);
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при смене пароля пользователя {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Управление сессиями

        public async Task<PagedResponseDto<SimpleUserSessionDto>> GetAllSessionsAsync(BasePagedRequest request)
        {
            try
            {
                var query = _context.UserSessions
                    .Include(s => s.User)
                    .AsQueryable();

                // Поиск по имени пользователя или email
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(s =>
                        s.User.FirstName.Contains(request.SearchTerm) ||
                        s.User.LastName.Contains(request.SearchTerm) ||
                        s.User.Email.Contains(request.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                var sessions = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new SimpleUserSessionDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        UserFullName = s.User.FirstName + " " + s.User.LastName,
                        CreatedAt = s.CreatedAt,
                        ExpiresAt = s.ExpiresAt,
                        IsRevoked = s.IsRevoked,
                        DeviceInfo = s.DeviceInfo,
                        IpAddress = s.IpAddress,
                        IsActive = !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow
                    })
                    .ToListAsync();

                return new PagedResponseDto<SimpleUserSessionDto>
                {
                    Data = sessions,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессий");
                return new PagedResponseDto<SimpleUserSessionDto>();
            }
        }

        public async Task<List<SimpleUserSessionDto>> GetUserSessionsAsync(string userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new SimpleUserSessionDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        UserFullName = s.User.FirstName + " " + s.User.LastName,
                        CreatedAt = s.CreatedAt,
                        ExpiresAt = s.ExpiresAt,
                        IsRevoked = s.IsRevoked,
                        DeviceInfo = s.DeviceInfo,
                        IpAddress = s.IpAddress,
                        IsActive = !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow
                    })
                    .ToListAsync();

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессий пользователя {UserId}", userId);
                return new List<SimpleUserSessionDto>();
            }
        }

        public async Task<bool> RevokeSessionAsync(int sessionId)
        {
            try
            {
                var session = await _context.UserSessions.FindAsync(sessionId);
                if (session == null)
                    return false;

                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве сессии {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> RevokeAllUserSessionsAsync(string userId)
        {
            try
            {
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && !s.IsRevoked)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве всех сессий пользователя {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Логи активности

        public async Task<PagedResponseDto<SimpleActivityLogDto>> GetActivityLogsAsync(BasePagedRequest request)
        {
            try
            {
                var query = _context.ActivityLogs
                    .Include(a => a.User)
                    .AsQueryable();

                // Поиск по имени пользователя или email
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(a =>
                        a.User.FirstName.Contains(request.SearchTerm) ||
                        a.User.LastName.Contains(request.SearchTerm) ||
                        a.User.Email.Contains(request.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                var logs = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(a => new SimpleActivityLogDto
                    {
                        Id = a.Id,
                        UserFullName = a.User.FirstName + " " + a.User.LastName,
                        UserEmail = a.User.Email ?? "",
                        Action = a.Action,
                        Success = a.Success,
                        Timestamp = a.Timestamp,
                        IpAddress = a.IpAddress,
                        DeviceType = a.DeviceType
                    })
                    .ToListAsync();

                return new PagedResponseDto<SimpleActivityLogDto>
                {
                    Data = logs,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности");
                return new PagedResponseDto<SimpleActivityLogDto>();
            }
        }

        public async Task<List<SimpleActivityLogDto>> GetUserActivityLogsAsync(string userId, int limit = 50)
        {
            try
            {
                var logs = await _context.ActivityLogs
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new SimpleActivityLogDto
                    {
                        Id = a.Id,
                        UserFullName = a.User.FirstName + " " + a.User.LastName,
                        UserEmail = a.User.Email ?? "",
                        Action = a.Action,
                        Success = a.Success,
                        Timestamp = a.Timestamp,
                        IpAddress = a.IpAddress,
                        DeviceType = a.DeviceType
                    })
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности пользователя {UserId}", userId);
                return new List<SimpleActivityLogDto>();
            }
        }

        #endregion

        #region Статистика

        public async Task<SimpleStatsDto> GetSimpleStatisticsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = now.Date;

                var stats = new SimpleStatsDto
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                    InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive),
                    TotalSessions = await _context.UserSessions.CountAsync(s => !s.IsRevoked && s.ExpiresAt > now),
                    TodaysLogins = await _context.ActivityLogs.CountAsync(a =>
                        a.Action == ActivityAction.Login && a.Success && a.Timestamp >= today),
                    TodaysRegistrations = await _context.ActivityLogs.CountAsync(a =>
                        a.Action == ActivityAction.Register && a.Success && a.Timestamp >= today)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики");
                return new SimpleStatsDto();
            }
        }

        #endregion
    }
}