// Controllers/SessionsController.cs
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
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<SessionsController> _logger;

        public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserSessions()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized();

                var sessions = await _sessionService.GetUserSessionsAsync(userId);
                return Ok(new { success = true, data = sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессий пользователя");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{refreshToken}")]
        public async Task<IActionResult> GetSessionByToken(string refreshToken)
        {
            try
            {
                var session = await _sessionService.GetSessionByRefreshTokenAsync(refreshToken);
                if (session == null)
                    return NotFound(new { success = false, message = "Сессия не найдена" });

                // Проверяем что сессия принадлежит текущему пользователю
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // Здесь нужно добавить проверку владения сессией

                return Ok(new { success = true, data = session });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессии по токену");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("{sessionId}/revoke")]
        public async Task<IActionResult> RevokeSession(int sessionId)
        {
            try
            {
                var result = await _sessionService.RevokeSessionByIdAsync(sessionId);
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

        [HttpPost("revoke-all")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized();

                var result = await _sessionService.RevokeAllUserSessionsAsync(userId);
                return Ok(new { success = result, message = result ? "Все сессии отозваны" : "Ошибка при отзыве сессий" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве всех сессий пользователя");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPut("{sessionId}")]
        public async Task<IActionResult> UpdateSession(int sessionId, [FromBody] UpdateSessionDto updateSessionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _sessionService.UpdateSessionAsync(sessionId, updateSessionDto);
                if (!result)
                    return NotFound(new { success = false, message = "Сессия не найдена" });

                return Ok(new { success = true, message = "Сессия обновлена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении сессии {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("cleanup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupExpiredSessions()
        {
            try
            {
                var cleanedCount = await _sessionService.CleanupExpiredSessionsAsync();
                return Ok(new { success = true, message = $"Очищено {cleanedCount} истекших сессий" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке истекших сессий");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{refreshToken}/validate")]
        public async Task<IActionResult> ValidateSession(string refreshToken)
        {
            try
            {
                var isValid = await _sessionService.IsSessionValidAsync(refreshToken);
                return Ok(new { success = true, isValid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при валидации сессии");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }
    }
}