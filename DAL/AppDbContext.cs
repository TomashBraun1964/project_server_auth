using DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Настройка префиксов таблиц Identity
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName != null && tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }

            // Настройка для ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .HasComment("Дата создания аккаунта");
                entity.Property(e => e.FirstName)
                      .IsRequired()
                      .HasMaxLength(100)
                      .HasComment("Имя пользователя");
                entity.Property(e => e.LastName)
                      .IsRequired()
                      .HasMaxLength(100)
                      .HasComment("Фамилия пользователя");
                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true)
                      .HasComment("Активен ли пользователь");
                entity.Property(e => e.Avatar)
                      .HasMaxLength(255)
                      .HasComment("Путь к аватару пользователя");
                entity.Property(e => e.Department)
                      .HasMaxLength(100)
                      .HasComment("Отдел пользователя");
                entity.Property(e => e.EmailConfirmedAt)
                      .HasComment("Дата подтверждения email");
                entity.Property(e => e.TwoFactorEnabled)
                      .HasDefaultValue(false)
                      .HasComment("Включена ли двухфакторная аутентификация");
                entity.Property(e => e.UpdatedAt)
                      .HasComment("Дата последнего обновления");
                entity.Property(e => e.LastLogin)
                      .HasComment("Дата последнего входа");
                // OAuth поля
                entity.Property(e => e.ExternalProvider)
                      .HasMaxLength(50)
                      .HasComment("Внешний провайдер OAuth (Google, Facebook)");
                entity.Property(e => e.ExternalId)
                      .HasMaxLength(100)
                      .HasComment("ID пользователя у внешнего провайдера");
                entity.Property(e => e.IsExternalAccount)
                      .HasDefaultValue(false)
                      .HasComment("Создан ли аккаунт через внешнего провайдера");
                // Индексы
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Email");
                entity.HasIndex(e => e.IsActive)
                      .HasDatabaseName("IX_Users_IsActive");
                entity.HasIndex(e => e.CreatedAt)
                      .HasDatabaseName("IX_Users_CreatedAt");
                entity.HasIndex(e => new { e.IsActive, e.CreatedAt })
                      .HasDatabaseName("IX_Users_IsActive_CreatedAt");
                entity.HasIndex(e => e.TwoFactorEnabled)
                      .HasDatabaseName("IX_Users_TwoFactorEnabled");
                entity.HasIndex(e => e.Department)
                      .HasDatabaseName("IX_Users_Department");
                entity.HasIndex(e => e.LastLogin)
                      .HasDatabaseName("IX_Users_LastLogin");
                // OAuth индексы
                entity.HasIndex(e => new { e.ExternalProvider, e.ExternalId })
                      .IsUnique()
                      .HasFilter("\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL") // Исправлен фильтр с кавычками
                      .HasDatabaseName("IX_Users_ExternalProvider_ExternalId");
                entity.HasIndex(e => e.ExternalProvider)
                      .HasDatabaseName("IX_Users_ExternalProvider");
                entity.HasIndex(e => e.IsExternalAccount)
                      .HasDatabaseName("IX_Users_IsExternalAccount");
                // Связи
                entity.HasMany(u => u.UserSessions)
                      .WithOne(s => s.User)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(u => u.ActivityLogs)
                      .WithOne(a => a.User)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка для UserSession
            builder.Entity<UserSession>(entity =>
            {
                entity.ToTable("UserSessions");
                entity.Property(e => e.Id)
                      .HasComment("Уникальный идентификатор сессии");
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .HasComment("Дата создания сессии");
                entity.Property(e => e.RefreshToken)
                      .IsRequired()
                      .HasMaxLength(500)
                      .HasComment("Refresh токен");
                entity.Property(e => e.ExpiresAt)
                      .IsRequired()
                      .HasComment("Дата окончания действия токена");
                entity.Property(e => e.IsRevoked)
                      .HasDefaultValue(false)
                      .HasComment("Отозван ли токен");
                entity.Property(e => e.RevokedAt)
                      .HasComment("Время аннулирования сессии");
                entity.Property(e => e.DeviceInfo)
                      .HasMaxLength(500)
                      .HasComment("Информация об устройстве (браузер, ОС)");
                entity.Property(e => e.IpAddress)
                      .HasMaxLength(45)
                      .HasComment("IP адрес создания сессии");
                entity.Property(e => e.UserAgent)
                      .HasMaxLength(500)
                      .HasComment("User Agent браузера");
                // Индексы
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_UserSessions_UserId");
                entity.HasIndex(e => new { e.RefreshToken, e.IsRevoked })
                      .IsUnique()
                      .HasDatabaseName("IX_UserSessions_RefreshToken_IsRevoked");
                entity.HasIndex(e => e.ExpiresAt)
                      .HasDatabaseName("IX_UserSessions_ExpiresAt");
                entity.HasIndex(e => e.IsRevoked)
                      .HasDatabaseName("IX_UserSessions_IsRevoked");
                entity.HasIndex(e => new { e.UserId, e.IsRevoked, e.ExpiresAt })
                      .HasDatabaseName("IX_UserSessions_UserId_IsRevoked_ExpiresAt");
            });

            // Настройка для ActivityLog
            builder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("ActivityLogs");
                entity.Property(e => e.Id)
                      .HasComment("Уникальный идентификатор записи лога");
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .HasComment("Дата создания записи");
                entity.Property(e => e.Timestamp)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .HasComment("Время выполнения действия");
                entity.Property(e => e.Action)
                      .IsRequired()
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasComment("Тип действия");
                entity.Property(e => e.Success)
                      .HasDefaultValue(true)
                      .HasComment("Успешно ли выполнено действие");
                entity.Property(e => e.EntityType)
                      .HasMaxLength(100)
                      .HasComment("Тип сущности над которой выполнено действие");
                entity.Property(e => e.EntityId)
                      .HasMaxLength(50)
                      .HasComment("ID сущности над которой выполнено действие");
                entity.Property(e => e.Details)
                      .HasMaxLength(500)
                      .HasComment("Дополнительная информация о действии");
                entity.Property(e => e.DeviceType)
                      .HasConversion<int>()
                      .HasComment("Тип устройства, с которого выполнено действие");
                entity.Property(e => e.IpAddress)
                      .HasMaxLength(45)
                      .HasComment("IP адрес откуда выполнено действие");
                entity.Property(e => e.UserAgent)
                      .HasMaxLength(500)
                      .HasComment("User Agent браузера");
                // Индексы
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_ActivityLogs_UserId");
                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_ActivityLogs_Timestamp");
                entity.HasIndex(e => e.Action)
                      .HasDatabaseName("IX_ActivityLogs_Action");
                entity.HasIndex(e => e.Success)
                      .HasDatabaseName("IX_ActivityLogs_Success");
                entity.HasIndex(e => new { e.UserId, e.Timestamp })
                      .HasDatabaseName("IX_ActivityLogs_UserId_Timestamp");
                entity.HasIndex(e => new { e.Action, e.Timestamp })
                      .HasDatabaseName("IX_ActivityLogs_Action_Timestamp");
                entity.HasIndex(e => new { e.Success, e.Timestamp })
                      .HasDatabaseName("IX_ActivityLogs_Success_Timestamp");
                entity.HasIndex(e => e.EntityType)
                      .HasDatabaseName("IX_ActivityLogs_EntityType");
                entity.HasIndex(e => new { e.EntityType, e.EntityId })
                      .HasDatabaseName("IX_ActivityLogs_EntityType_EntityId");
                entity.HasIndex(e => e.DeviceType)
                      .HasDatabaseName("IX_ActivityLogs_DeviceType");
            });

            // Настройка для Identity таблиц
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(entity =>
            {
                entity.ToTable("Roles");
            });
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });
        }

        /// <summary>
        /// Переопределение SaveChanges для автоматического обновления полей аудита
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Переопределение SaveChangesAsync для автоматического обновления полей аудита
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Обновление полей аудита перед сохранением
        /// </summary>
        private void UpdateAuditFields()
        {
            var userEntries = ChangeTracker.Entries<ApplicationUser>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in userEntries)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            // Автоматическое проставление RevokedAt для сессий
            var sessionEntries = ChangeTracker.Entries<UserSession>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in sessionEntries)
            {
                var session = entry.Entity;
                if (session.IsRevoked && session.RevokedAt == null)
                {
                    session.RevokedAt = DateTime.UtcNow;
                }
            }
        }
    }
}