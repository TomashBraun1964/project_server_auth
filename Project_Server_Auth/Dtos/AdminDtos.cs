// Dtos/SimpleAdminDtos.cs
using DAL.Models;
using System.ComponentModel.DataAnnotations;

namespace Project_Server_Auth.Dtos
{
    // DTO для создания пользователя администратором (простая версия)
    public class SimpleAdminCreateUserDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    // DTO для обновления пользователя администратором (простая версия)
    public class SimpleAdminUpdateUserDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        public bool? IsActive { get; set; }
    }

    // DTO для списка пользователей в админке
    public class SimpleAdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public int ActiveSessionsCount { get; set; }
    }

    // DTO для простой статистики
    public class SimpleStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int TotalSessions { get; set; }
        public int TodaysLogins { get; set; }
        public int TodaysRegistrations { get; set; }
    }

    // DTO для логов активности (упрощенная версия)
    public class SimpleActivityLogDto
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public ActivityAction Action { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public DeviceType DeviceType { get; set; }
    }

    // DTO для сессий пользователя (упрощенная версия)
    public class SimpleUserSessionDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public bool IsActive { get; set; }
    }

    // DTO для смены пароля администратором
    public class AdminChangePasswordDto
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}