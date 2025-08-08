using DKyThucTap.Models.DTOs;

namespace DKyThucTap.Services
{
    public interface IOnlineUserService
    {
        Task<int> GetOnlineUserCountAsync();
        Task<List<OnlineUserDto>> GetOnlineUsersAsync();
        Task AddUserConnectionAsync(string connectionId, int userId, string? clientInfo = null);
        Task RemoveUserConnectionAsync(string connectionId);
        Task UpdateUserActivityAsync(string connectionId);
        Task CleanupInactiveConnectionsAsync();
        Task<bool> IsUserOnlineAsync(int userId);
    }

    public class OnlineUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public string? ClientInfo { get; set; }
        public int ConnectionCount { get; set; } // Số lượng kết nối của user (có thể có nhiều tab/device)
    }
}
