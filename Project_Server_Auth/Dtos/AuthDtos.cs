using DAL.Models;
using System.ComponentModel.DataAnnotations;

namespace Project_Server_Auth.Dtos
{ // DTO для результата поиска пользователей
    public class UserSearchResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Avatar { get; set; }
        public bool IsActive { get; set; }
    }
    public class LogoutDto
    {
        public string? RefreshToken { get; set; }
    }

    // DTO для регистрации нового пользователя
    public class RegisterDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [MaxLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [MaxLength(100, ErrorMessage = "Фамилия не должна превышать 100 символов")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Пароль должен содержать минимум одну заглавную букву, одну строчную букву и одну цифру")]
        public string Password { get; set; } = string.Empty;
    }

    // DTO для входа пользователя
    public class LoginDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }

    // DTO для возврата JWT-токена
    public class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    // DTO для обновления refresh-токена (шаг 2)
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh-токен обязателен")]
        [MaxLength(500, ErrorMessage = "Refresh-токен не должен превышать 500 символов")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Расширенный DTO для токена с дополнительной информацией
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserProfileDto User { get; set; } = new();
        public bool RequiresTwoFactor { get; set; } = false;
    }

    // DTO для профиля пользователя (шаг 3)
    public class UserProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Avatar { get; set; } // URL аватара
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    // DTO для обновления данных пользователя (шаг 3)
    public class UpdateUserDto
    {
        [MaxLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
        public string? FirstName { get; set; }

        [MaxLength(100, ErrorMessage = "Фамилия не должна превышать 100 символов")]
        public string? LastName { get; set; }

        [MaxLength(255, ErrorMessage = "URL аватара не должен превышать 255 символов")]
        [Url(ErrorMessage = "Некорректный формат URL аватара")]
        public string? Avatar { get; set; }

        [MaxLength(100, ErrorMessage = "Отдел не должен превышать 100 символов")]
        public string? Department { get; set; }
    }

    // DTO для внешней аутентификации (шаг 4, OAuth)
    public class ExternalLoginDto
    {
        [Required(ErrorMessage = "Имя провайдера обязательно")]
        [MaxLength(50, ErrorMessage = "Имя провайдера не должен превышать 50 символов")]
        public string Provider { get; set; } = string.Empty;

        [Required(ErrorMessage = "Токен провайдера обязателен")]
        [MaxLength(1000, ErrorMessage = "Токен провайдера не должен превышать 1000 символов")]
        public string Token { get; set; } = string.Empty;
    }

    // DTO для включения/выключения 2FA (шаг 3)
    public class TwoFactorDto
    {
        [Required(ErrorMessage = "Метод 2FA обязателен")]
        public bool Enable { get; set; } // true для включения, false для выключения

        [MaxLength(6, ErrorMessage = "Код подтверждения не должен превышать 6 символов")]
        public string? VerificationCode { get; set; } // Код для подтверждения 2FA
    }

    // DTO для массовых операций (шаг 5, админ-функции)
    public class BulkOperationDto
    {
        [Required(ErrorMessage = "Список идентификаторов обязателен")]
        public List<string> UserIds { get; set; } = new List<string>();

        [Required(ErrorMessage = "Тип операции обязателен")]
        public BulkOperationType OperationType { get; set; }

        // Дополнительные параметры для операций
        public string? NewRole { get; set; } // Для операции ChangeRole
        public string? Reason { get; set; } // Причина операции
    }

    // DTO для возврата данных о сессии пользователя
    public class UserSessionDto
    {
        public int Id { get; set; } // Добавить ID для удобства управления
        [MaxLength(500, ErrorMessage = "Refresh-токен не должен превышать 500 символов")]
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        [MaxLength(500, ErrorMessage = "Информация об устройстве не должна превышать 500 символов")]
        public string? DeviceInfo { get; set; } // Информация о браузере или ОС
    }

    // DTO для обновления данных сессии
    public class UpdateSessionDto
    {
        [MaxLength(500, ErrorMessage = "Refresh-токен не должен превышать 500 символов")]
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool? IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        [MaxLength(500, ErrorMessage = "Информация об устройстве не должна превышать 500 символов")]
        public string? DeviceInfo { get; set; } // Обновление данных об устройстве
    }

    // DTO для получения логов активности
    public class ActivityLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty; // Для удобства отображения
        public ActivityAction Action { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public DeviceType DeviceType { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    // DTO для фильтрации логов активности
    public class ActivityLogFilterDto : BasePagedRequest
    {
        public string? UserId { get; set; }
        public ActivityAction? Action { get; set; }
        public bool? Success { get; set; }
        public DeviceType? DeviceType { get; set; }
        public string? EntityType { get; set; }
    }

    // DTO для смены пароля
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Текущий пароль обязателен")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Пароль должен содержать минимум одну заглавную букву, одну строчную букву и одну цифру")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // DTO для восстановления пароля
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;
    }

    // DTO для сброса пароля
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Токен сброса обязателен")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Пароль должен содержать минимум одну заглавную букву, одну строчную букву и одну цифру")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Универсальный DTO для пагинированного ответа
    public class PagedResponseDto<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    // DTO для списка пользователей в админке
    public class UserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsExternalAccount { get; set; }
        public string? ExternalProvider { get; set; }
    }

    // DTO для фильтрации пользователей
    public class UserFilterDto : BasePagedRequest
    {
        public bool? IsActive { get; set; }
        public string? Department { get; set; }
        public bool? IsExternalAccount { get; set; }
        public string? ExternalProvider { get; set; }
    }

    // DTO для статистики пользователей (админ панель)
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int ExternalAccounts { get; set; }
        public int UsersWithTwoFactor { get; set; }
        public Dictionary<string, int> UsersByDepartment { get; set; } = new();
        public List<DailyRegistrationDto> RegistrationTrend { get; set; } = new();
    }

    // DTO для ежедневной статистики регистраций
    public class DailyRegistrationDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}