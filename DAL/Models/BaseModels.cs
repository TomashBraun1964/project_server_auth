using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Базовая модель с аудитом (для моделей с Guid Id)
    /// </summary>
    public abstract class BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Базовая модель для активности пользователя (с int Id для совместимости с существующими моделями)
    /// </summary>
    public abstract class BaseUserActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Базовый ответ API
    /// </summary>
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public object? Data { get; set; } // Добавлено для возврата данных в успешных ответах
    }

    /// <summary>
    /// Базовый запрос с пагинацией
    /// </summary>
    public class BasePagedRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 1000, ErrorMessage = "Размер страницы должен быть от 1 до 1000")]
        public int PageSize { get; set; } = 20;

        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        public DateTime? StartDate { get; set; } // Добавлено для фильтрации по начальной дате
        public DateTime? EndDate { get; set; } // Добавлено для фильтрации по конечной дате
    }

    // ================== ENUMS ==================

    /// <summary>
    /// Типы действий пользователя для логирования (расширенный с OAuth)
    /// </summary>
    public enum ActivityAction
    {
        Login,
        Logout,
        Register,
        ForgotPassword,
        ResetPassword,
        ChangePassword,
        UpdateProfile,
        CreateUser,
        UpdateUser,
        BlockUser,
        UnblockUser,
        DeleteUser,
        UpdateSettings,
        ViewLogs,
        ExternalLogin,
        LinkExternalAccount,
        UnlinkExternalAccount,
        ExternalRegister
    }

    /// <summary>
    /// Типы массовых операций
    /// </summary>
    public enum BulkOperationType
    {
        Block,
        Unblock,
        Delete,
        ChangeRole,
        ForceLogout,
        ResetPassword,
        ConfirmEmail
    }

    /// <summary>
    /// Методы двухфакторной аутентификации
    /// </summary>
    public enum TwoFactorMethod
    {
        SMS,
        Email,
        AuthenticatorApp,
        BackupCodes
    }

    /// <summary>
    /// Действия с доверенными устройствами
    /// </summary>
    public enum TrustedDeviceAction
    {
        Remove,
        Rename,
        Block
    }

    /// <summary>
    /// Типы предупреждений безопасности
    /// </summary>
    public enum SecurityAlertType
    {
        MultipleFailedLogins,
        SuspiciousLocation,
        UnusualActivity,
        BruteForceAttempt,
        AccountLocked,
        PasswordResetRequest,
        NewDeviceLogin
    }

    /// <summary>
    /// Уровни риска безопасности
    /// </summary>
    public enum SecurityRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Типы устройств
    /// </summary>
    public enum DeviceType
    {
        Unknown,
        Desktop,
        Mobile,
        Tablet
    }
}