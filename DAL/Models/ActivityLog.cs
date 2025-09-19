using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DAL.Models
{
    // Класс для логирования действий пользователя, наследуется от BaseUserActivity
    public class ActivityLog : BaseUserActivity
    {
        // Обязательное поле для типа действия, использует enum ActivityAction
        [Required]
        public ActivityAction Action { get; set; }

        // Тип сущности, связанной с действием, опционально, максимальная длина 100 символов
        [MaxLength(100)]
        public string? EntityType { get; set; }

        // Идентификатор сущности, опционально, максимальная длина 50 символов
        [MaxLength(50)]
        public string? EntityId { get; set; }

        // Детали действия, опционально, максимальная длина 500 символов
        [MaxLength(500)]
        public string? Details { get; set; }

        // Флаг успешного выполнения действия, по умолчанию true
        public bool Success { get; set; } = true;

        // Время выполнения действия, устанавливается автоматически
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Тип устройства, с которого выполнено действие, по умолчанию Unknown
        public DeviceType DeviceType { get; set; } = DeviceType.Unknown; // Добавлено для указания типа устройства

        // Навигационное свойство для связи с пользователем
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}