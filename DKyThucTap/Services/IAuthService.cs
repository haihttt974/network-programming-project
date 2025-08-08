using DKyThucTap.Models;
using DKyThucTap.Models.DTOs;

namespace DKyThucTap.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User)> LoginAsync(LoginDto loginDto);
        Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterDto registerDto);
        Task<bool> LogoutAsync(int userId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UpdateLastLoginAsync(int userId);
        Task<UserProfileDto?> GetUserProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileDto updateDto);
        Task<bool> IsEmailExistsAsync(string email);
        Task<List<Role>> GetRolesAsync();
        bool VerifyPassword(string password, string hash);
        string HashPassword(string password);
    }
}
