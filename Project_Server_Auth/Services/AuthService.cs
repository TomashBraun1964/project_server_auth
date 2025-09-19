// Services/AuthService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DAL;
using DAL.Models;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Проверка существования пользователя
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                throw new InvalidOperationException("Пользователь с таким email уже существует");
            }

            // Создание пользователя
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Ошибка создания пользователя: {errors}");
            }

            // Логирование
            await LogActivityAsync(user.Id, ActivityAction.Register, true);

            // Генерация токенов
            var tokens = await GenerateTokensAsync(user);

            return new AuthResponseDto
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserProfileDto(user)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                await LogActivityAsync(null, ActivityAction.Login, false, "Пользователь не найден");
                throw new UnauthorizedAccessException("Неверные учетные данные");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, true);
            if (!result.Succeeded)
            {
                await LogActivityAsync(user.Id, ActivityAction.Login, false, "Неверный пароль");
                throw new UnauthorizedAccessException("Неверные учетные данные");
            }

            // Обновление последнего входа
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await LogActivityAsync(user.Id, ActivityAction.Login, true);

            var tokens = await GenerateTokensAsync(user);

            return new AuthResponseDto
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserProfileDto(user)
            };
        }

        public async Task<bool> LogoutAsync(string userId, string? refreshToken = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var session = await _context.UserSessions
                        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && !s.IsRevoked);

                    if (session != null)
                    {
                        session.IsRevoked = true;
                        session.RevokedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                await LogActivityAsync(userId, ActivityAction.Logout, true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя {UserId}", userId);
                return false;
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshTokenDto.RefreshToken && !s.IsRevoked);

            if (session == null || session.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Недействительный refresh токен");
            }

            // Отзыв старого токена
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;

            // Генерация новых токенов
            var tokens = await GenerateTokensAsync(session.User);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserProfileDto(session.User)
            };
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Пользователь не найден");

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                await LogActivityAsync(userId, ActivityAction.ChangePassword, false);
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Ошибка смены пароля: {errors}");
            }

            await LogActivityAsync(userId, ActivityAction.ChangePassword, true);
            return true;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null ? MapToUserProfileDto(user) : null;
        }

        // Приватные методы
        private async Task<TokenResult> GenerateTokensAsync(ApplicationUser user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 1 час

            // Сохранение сессии
            var session = new UserSession
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 дней для refresh token
                IsRevoked = false
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return new TokenResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };
        }

        private string GenerateAccessToken(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "your-secret-key-here");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
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

        private async Task LogActivityAsync(string? userId, ActivityAction action, bool success, string? details = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    UserId = userId ?? "Unknown",
                    Action = action,
                    Success = success,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при логировании активности");
            }
        }

        private class TokenResult
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }
    }
}