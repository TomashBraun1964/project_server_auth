// Services/Interfaces/ITokenService.cs
using System.Security.Claims;
using DAL.Models;

namespace Project_Server_Auth.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetClaimsFromToken(string token);
        bool ValidateToken(string token);
        DateTime GetTokenExpiration(string token);
        string? GetUserIdFromToken(string token);
    }
}