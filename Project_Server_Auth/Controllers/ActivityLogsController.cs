// Controllers/ActivityLogsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<ActivityLogsController> _logger;

        public ActivityLogsController(IActivityLogService activityLogService, ILogger<ActivityLogsController> logger)
        {
            _activityLogService = activityLogService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogFilterDto filter)
        {
            try
            {
                var result = await _activityLogService.GetActivityLogsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActivityLog(int id)
        {
            try
            {
                var log = await _activityLogService.GetActivityLogByIdAsync(id);
                if (log == null)
                    return NotFound(new { success = false, message = "Лог не найден" });

                return Ok(new { success = true, data = log });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении лога активности {Id}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserActivityLogs(string userId, [FromQuery] int limit = 50)
        {
            try
            {
                // Проверяем что пользователь запрашивает свои логи или является админом
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                if (currentUserId != userId && !isAdmin)
                    return Forbid("Можно просматривать только собственные логи активности");

                var logs = await _activityLogService.GetUserActivityLogsAsync(userId, limit);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности пользователя {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyActivityLogs([FromQuery] int limit = 50)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized();

                var logs = await _activityLogService.GetUserActivityLogsAsync(userId, limit);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении собственных логов активности");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("recent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 20)
        {
            try
            {
                var logs = await _activityLogService.GetRecentActivitiesAsync(limit);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении последних активностей");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActivityStatistics([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            try
            {
                var statistics = await _activityLogService.GetActivityStatisticsAsync(from, to);
                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики активности");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("cleanup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
        {
            try
            {
                var cleanedCount = await _activityLogService.CleanupOldLogsAsync(daysToKeep);
                return Ok(new { success = true, message = $"Очищено {cleanedCount} старых логов (старше {daysToKeep} дней)" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке старых логов");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }
    }
}