// Services/TokenService.cs
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DAL.Models;
using Project_Server_Auth.Services.Interfaces;

namespace Project_Server_Auth.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateAccessToken(ApplicationUser user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(GetSecretKey());

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim("FirstName", user.FirstName),
                    new Claim("LastName", user.LastName),
                    new Claim("IsActive", user.IsActive.ToString())
                };

                if (!string.IsNullOrEmpty(user.Department))
                    claims.Add(new Claim("Department", user.Department));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(GetExpirationMinutes()),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = GetIssuer(),
                    Audience = GetAudience()
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации access токена для пользователя {UserId}", user.Id);
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                var randomBytes = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации refresh токена");
                throw;
            }
        }

        public ClaimsPrincipal? GetClaimsFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(GetSecretKey());

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = GetIssuer(),
                    ValidateAudience = true,
                    ValidAudience = GetAudience(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при извлечении claims из токена");
                return null;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var claims = GetClaimsFromToken(token);
                return claims != null;
            }
            catch
            {
                return false;
            }
        }

        public DateTime GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при получении срока действия токена");
                return DateTime.MinValue;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            try
            {
                var claims = GetClaimsFromToken(token);
                return claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при извлечении UserId из токена");
                return null;
            }
        }

        private string GetSecretKey()
        {
            return _configuration["JwtSettings:SecretKey"] ?? "your-secret-key-here";
        }

        private int GetExpirationMinutes()
        {
            return int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out var minutes) ? minutes : 60;
        }

        private string GetIssuer()
        {
            return _configuration["JwtSettings:Issuer"] ?? "YourApp";
        }

        private string GetAudience()
        {
            return _configuration["JwtSettings:Audience"] ?? "YourApp";
        }
    }
}