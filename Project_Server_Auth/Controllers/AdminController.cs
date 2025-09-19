// Controllers/AdminPanelController.cs
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Server_Auth.Dtos;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Controllers
{
    [ApiController]
    [Route("api/admin-panel")]
    [Authorize(Roles = "Admin")]
    public class AdminPanelController : ControllerBase
    {
        private readonly ISimpleAdminService _adminService;
        private readonly ILogger<AdminPanelController> _logger;

        public AdminPanelController(ISimpleAdminService adminService, ILogger<AdminPanelController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        #region Пользователи

        /// <summary>
        /// Получить список всех пользователей с пагинацией и поиском
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] BasePagedRequest request)
        {
            try
            {
                var result = await _adminService.GetUsersAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить информацию о конкретном пользователе
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Создать нового пользователя
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] SimpleAdminCreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _adminService.CreateUserAsync(createUserDto);
                if (!result)
                    return BadRequest(new { success = false, message = "Не удалось создать пользователя" });

                return Ok(new { success = true, message = "Пользователь успешно создан" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании пользователя");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Обновить данные пользователя
        /// </summary>
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] SimpleAdminUpdateUserDto updateUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _adminService.UpdateUserAsync(id, updateUserDto);
                if (!result)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                return Ok(new { success = true, message = "Пользователь успешно обновлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Удалить пользователя
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                return Ok(new { success = true, message = "Пользователь успешно удален" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Активировать пользователя
        /// </summary>
        [HttpPost("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            try
            {
                var result = await _adminService.ActivateUserAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                return Ok(new { success = true, message = "Пользователь активирован" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при активации пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Деактивировать пользователя
        /// </summary>
        [HttpPost("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            try
            {
                var result = await _adminService.DeactivateUserAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                return Ok(new { success = true, message = "Пользователь деактивирован" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при деактивации пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Сменить пароль пользователя
        /// </summary>
        [HttpPost("users/{id}/change-password")]
        public async Task<IActionResult> ChangeUserPassword(string id, [FromBody] AdminChangePasswordDto passwordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _adminService.ChangeUserPasswordAsync(id, passwordDto.NewPassword);
                if (!result)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                return Ok(new { success = true, message = "Пароль пользователя изменен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при смене пароля пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        #endregion

        #region Сессии

        /// <summary>
        /// Получить все активные сессии
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetAllSessions([FromQuery] BasePagedRequest request)
        {
            try
            {
                var result = await _adminService.GetAllSessionsAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессий");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить сессии конкретного пользователя
        /// </summary>
        [HttpGet("users/{id}/sessions")]
        public async Task<IActionResult> GetUserSessions(string id)
        {
            try
            {
                var sessions = await _adminService.GetUserSessionsAsync(id);
                return Ok(new { success = true, data = sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессий пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Отозвать конкретную сессию
        /// </summary>
        [HttpPost("sessions/{sessionId}/revoke")]
        public async Task<IActionResult> RevokeSession(int sessionId)
        {
            try
            {
                var result = await _adminService.RevokeSessionAsync(sessionId);
                if (!result)
                    return NotFound(new { success = false, message = "Сессия не найдена" });

                return Ok(new { success = true, message = "Сессия отозвана" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве сессии {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Отозвать все сессии пользователя
        /// </summary>
        [HttpPost("users/{id}/sessions/revoke-all")]
        public async Task<IActionResult> RevokeAllUserSessions(string id)
        {
            try
            {
                var result = await _adminService.RevokeAllUserSessionsAsync(id);
                return Ok(new { success = result, message = result ? "Все сессии пользователя отозваны" : "Ошибка при отзыве сессий" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве всех сессий пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        #endregion

        #region Логи активности

        /// <summary>
        /// Получить логи активности всех пользователей
        /// </summary>
        [HttpGet("activity-logs")]
        public async Task<IActionResult> GetActivityLogs([FromQuery] BasePagedRequest request)
        {
            try
            {
                var result = await _adminService.GetActivityLogsAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить логи активности конкретного пользователя
        /// </summary>
        [HttpGet("users/{id}/activity-logs")]
        public async Task<IActionResult> GetUserActivityLogs(string id, [FromQuery] int limit = 50)
        {
            try
            {
                var logs = await _adminService.GetUserActivityLogsAsync(id, limit);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении логов активности пользователя {UserId}", id);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        #endregion

        #region Статистика

        /// <summary>
        /// Получить простую статистику системы
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _adminService.GetSimpleStatisticsAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        #endregion
    }
}