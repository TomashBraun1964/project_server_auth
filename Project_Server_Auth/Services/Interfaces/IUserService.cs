// Services/Interfaces/IUserService.cs
using Project_Server_Auth.Dtos;

namespace Project_Server_Auth.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetUserByIdAsync(string userId);
        Task<UserProfileDto?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
        Task<bool> DeactivateUserAsync(string userId);
        Task<bool> ActivateUserAsync(string userId);
        Task<PagedResponseDto<UserListItemDto>> GetUsersAsync(UserFilterDto filter);
        Task<UserStatisticsDto> GetUserStatisticsAsync();
        Task<bool> DeleteUserAsync(string userId);
        Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 10);
    }
}