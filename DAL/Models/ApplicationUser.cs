using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DAL.Models
{
    // Класс пользователя, расширяющий IdentityUser для поддержки кастомных полей
    public class ApplicationUser : IdentityUser
    {
        // Обязательное поле для имени пользователя, максимальная длина 100 символов
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        // Обязательное поле для фамилии пользователя, максимальная длина 100 символов
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Поле для хранения URL аватара пользователя, опционально, максимальная длина 255 символов
        [MaxLength(255)]
        public string? Avatar { get; set; } // Хранит URL аватара (рекомендуется для облачного хранения)

        // Поле для отдела пользователя, опционально, максимальная длина 100 символов
        [MaxLength(100)]
        public string? Department { get; set; }

        // Флаг активности пользователя, по умолчанию true
        public bool IsActive { get; set; } = true;

        // Дата и время создания записи, устанавливается автоматически
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Дата и время последнего входа, опционально
        public DateTime? LastLogin { get; set; }

        // Дата и время последнего обновления, опционально
        public DateTime? UpdatedAt { get; set; }

        // Дата и время подтверждения email, опционально, для будущей поддержки подтверждения
        public DateTime? EmailConfirmedAt { get; set; } // Добавлено для отслеживания подтверждения email

        // Флаг включения двухфакторной аутентификации, по умолчанию false, скрывает наследуемое свойство
        public new bool TwoFactorEnabled { get; set; } = false; // Добавлено для поддержки 2FA на шаге 3 ТЗ, использует new для избежания конфликта с IdentityUser

        // OAuth поля
        // Имя внешнего провайдера, опционально, максимальная длина 50 символов
        [MaxLength(50)]
        public string? ExternalProvider { get; set; }

        // Внешний идентификатор пользователя, опционально, максимальная длина 100 символов
        [MaxLength(100)]
        public string? ExternalId { get; set; }

        // Флаг, указывающий, является ли аккаунт внешним, по умолчанию false
        public bool IsExternalAccount { get; set; } = false;

        // Навигационные свойства
        // Коллекция сессий пользователя
        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

        // Коллекция логов активности пользователя
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

        // Вычисляемые свойства
        // Полное имя пользователя, формируется из FirstName и LastName
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Проверка наличия пароля, учитывает внешние аккаунты
        [NotMapped]
        public bool HasPassword => !IsExternalAccount || !string.IsNullOrEmpty(PasswordHash);
    }
}