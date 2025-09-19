using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Security;
using System.Text.Json;

namespace Project_Server_Auth.Middleware
{
    public class AuthExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public AuthExceptionMiddleware(RequestDelegate next, ILogger<AuthExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.TraceIdentifier;
            _logger.LogError(exception, "Error occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                correlationId, context.Request.Path, context.Request.Method);

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = CreateErrorResponse(exception, correlationId);
            response.StatusCode = errorResponse.StatusCode;

            await response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }

        private ErrorResponseModel CreateErrorResponse(Exception exception, string correlationId)
        {
            return exception switch
            {
                // JWT и Security исключения
                SecurityTokenException or SecurityException => new ErrorResponseModel
                {
                    Success = false,
                    Message = "Invalid or expired token",
                    StatusCode = StatusCodes.Status401Unauthorized,
                    CorrelationId = correlationId
                },

                // Identity исключения
                InvalidOperationException when exception.Message.Contains("role") => new ErrorResponseModel
                {
                    Success = false,
                    Message = "Role operation failed",
                    StatusCode = StatusCodes.Status400BadRequest,
                    CorrelationId = correlationId
                },

                // Unauthorized
                UnauthorizedAccessException => new ErrorResponseModel
                {
                    Success = false,
                    Message = "Unauthorized access",
                    StatusCode = StatusCodes.Status401Unauthorized,
                    CorrelationId = correlationId
                },

                // Validation исключения
                ArgumentException or ArgumentNullException => new ErrorResponseModel
                {
                    Success = false,
                    Message = "Invalid request parameters",
                    StatusCode = StatusCodes.Status400BadRequest,
                    CorrelationId = correlationId
                },

                // Database исключения
                DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx => CreatePostgresErrorResponse(pgEx, correlationId),

                DbUpdateException => new ErrorResponseModel
                {
                    Success = false,
                    Message = "Database operation failed",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    CorrelationId = correlationId
                },

                // Операции с файлами
                FileNotFoundException => new ErrorResponseModel
                {
                    Success = false,
                    Message = "File not found",
                    StatusCode = StatusCodes.Status404NotFound,
                    CorrelationId = correlationId
                },

                DirectoryNotFoundException => new ErrorResponseModel
                {
                    Success = false,
                    Message = "Directory not found",
                    StatusCode = StatusCodes.Status404NotFound,
                    CorrelationId = correlationId
                },

                // Общие исключения
                _ => new ErrorResponseModel
                {
                    Success = false,
                    Message = _env.IsDevelopment() ? exception.Message : "An error occurred while processing your request",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    CorrelationId = correlationId
                }
            };
        }

        private ErrorResponseModel CreatePostgresErrorResponse(PostgresException pgEx, string correlationId)
        {
            return pgEx.SqlState switch
            {
                "23505" => new ErrorResponseModel // Unique violation
                {
                    Success = false,
                    Message = GetUniqueViolationMessage(pgEx.ConstraintName),
                    StatusCode = StatusCodes.Status409Conflict,
                    CorrelationId = correlationId
                },

                "23503" => new ErrorResponseModel // Foreign key violation
                {
                    Success = false,
                    Message = "Cannot delete record because it is referenced by other data",
                    StatusCode = StatusCodes.Status409Conflict,
                    CorrelationId = correlationId
                },

                "23514" => new ErrorResponseModel // Check constraint violation
                {
                    Success = false,
                    Message = "Data validation failed",
                    StatusCode = StatusCodes.Status400BadRequest,
                    CorrelationId = correlationId
                },

                _ => new ErrorResponseModel
                {
                    Success = false,
                    Message = _env.IsDevelopment() ? pgEx.Message : "Database operation failed",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    CorrelationId = correlationId
                }
            };
        }

        private static string GetUniqueViolationMessage(string? constraintName)
        {
            if (string.IsNullOrEmpty(constraintName))
                return "A record with these values already exists";

            return constraintName.ToLower() switch
            {
                var name when name.Contains("email") => "User with this email already exists",
                var name when name.Contains("username") => "Username is already taken",
                var name when name.Contains("refreshtoken") => "Token conflict occurred",
                _ => "A record with these values already exists"
            };
        }
    }

    public class ErrorResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}