using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DAL.Models
{
    // Класс для управления сессиями пользователя, наследуется от BaseUserActivity
    public class UserSession : BaseUserActivity
    {
        // Обязательное поле для refresh токена, максимальная длина 500 символов
        [Required]
        [MaxLength(500)]
        public string RefreshToken { get; set; } = string.Empty;

        // Обязательное поле для времени истечения токена
        [Required]
        public DateTime ExpiresAt { get; set; }

        // Флаг аннулирования сессии, по умолчанию false
        public bool IsRevoked { get; set; } = false;

        // Время аннулирования сессии, опционально
        public DateTime? RevokedAt { get; set; }

        // Информация об устройстве (браузер, ОС), опционально, максимальная длина 500 символов
        [MaxLength(500)]
        public string? DeviceInfo { get; set; } // Добавлено для хранения информации об устройстве

        // Навигационное свойство для связи с пользователем
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        // Вычисляемое свойство, указывающее, истек ли токен
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        // Вычисляемое свойство, указывающее, активна ли сессия
        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}