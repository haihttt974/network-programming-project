using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Services;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Services
{
    public class OnlineUserService : IOnlineUserService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<OnlineUserService> _logger;
        private readonly TimeSpan _inactiveThreshold = TimeSpan.FromMinutes(5); // 5 phút không hoạt động = offline

        public OnlineUserService(DKyThucTapContext context, ILogger<OnlineUserService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<bool> IsUserOnlineAsync(int userId)
        {
            // Kiểm tra nếu có ít nhất 1 kết nối active gần đây
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-2); // coi như offline nếu quá 2 phút
            return await _context.WebsocketConnections
                .AnyAsync(c => c.UserId == userId && c.LastActivity > cutoff);
        }

        public async Task<int> GetOnlineUserCountAsync()
        {
            try
            {
                // Cleanup inactive connections first
                await CleanupInactiveConnectionsAsync();

                // Calculate cutoff time before query to avoid LINQ translation issues
                var cutoffTime = DateTimeOffset.UtcNow.Subtract(_inactiveThreshold);

                // Count unique users with active connections
                var onlineCount = await _context.WebsocketConnections
                    .Where(wc => wc.LastActivity > cutoffTime)
                    .Select(wc => wc.UserId)
                    .Distinct()
                    .CountAsync();

                return onlineCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online user count");
                return 0;
            }
        }

        public async Task<List<OnlineUserDto>> GetOnlineUsersAsync()
        {
            try
            {
                // Cleanup inactive connections first
                await CleanupInactiveConnectionsAsync();

                // Calculate cutoff time before query to avoid LINQ translation issues
                var cutoffTime = DateTimeOffset.UtcNow.Subtract(_inactiveThreshold);

                // Get active connections with user data
                var activeConnections = await _context.WebsocketConnections
                    .Include(wc => wc.User)
                        .ThenInclude(u => u.UserProfile)
                    .Where(wc => wc.LastActivity > cutoffTime && wc.User != null)
                    .ToListAsync();

                // Group by user and create DTOs
                var onlineUsers = activeConnections
                    .GroupBy(wc => wc.UserId)
                    .Select(g => {
                        var firstConnection = g.First();
                        var user = firstConnection.User;

                        return new OnlineUserDto
                        {
                            UserId = g.Key,
                            UserName = user?.UserProfile != null
                                ? $"{user.UserProfile.FirstName} {user.UserProfile.LastName}".Trim()
                                : user?.Email ?? "Unknown User",
                            ProfilePictureUrl = user?.UserProfile?.ProfilePictureUrl,
                            LastActivity = g.Max(wc => wc.LastActivity),
                            ConnectionCount = g.Count(),
                            ClientInfo = firstConnection.ClientInfo
                        };
                    })
                    .OrderByDescending(u => u.LastActivity)
                    .ToList();

                return onlineUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users");
                return new List<OnlineUserDto>();
            }
        }

        public async Task AddUserConnectionAsync(string connectionId, int userId, string? clientInfo = null)
        {
            try
            {
                // Check if connection already exists
                var existingConnection = await _context.WebsocketConnections
                    .FirstOrDefaultAsync(wc => wc.ConnectionId == connectionId);

                if (existingConnection != null)
                {
                    _logger.LogWarning("Connection {ConnectionId} already exists, updating activity", connectionId);
                    existingConnection.LastActivity = DateTimeOffset.UtcNow;
                    existingConnection.ClientInfo = clientInfo;
                }
                else
                {
                    var connection = new WebsocketConnection
                    {
                        ConnectionId = connectionId,
                        UserId = userId,
                        ConnectedAt = DateTimeOffset.UtcNow,
                        LastActivity = DateTimeOffset.UtcNow,
                        ClientInfo = clientInfo
                    };

                    _context.WebsocketConnections.Add(connection);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added/Updated connection {ConnectionId} for user {UserId}", connectionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user connection {ConnectionId} for user {UserId}", connectionId, userId);
                throw; // Re-throw to let caller handle the error
            }
        }

        public async Task RemoveUserConnectionAsync(string connectionId)
        {
            try
            {
                var connection = await _context.WebsocketConnections
                    .FirstOrDefaultAsync(wc => wc.ConnectionId == connectionId);

                if (connection != null)
                {
                    _context.WebsocketConnections.Remove(connection);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Removed connection {ConnectionId} for user {UserId}", connectionId, connection.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user connection {ConnectionId}", connectionId);
            }
        }

        public async Task UpdateUserActivityAsync(string connectionId)
        {
            try
            {
                var connection = await _context.WebsocketConnections
                    .FirstOrDefaultAsync(wc => wc.ConnectionId == connectionId);

                if (connection != null)
                {
                    connection.LastActivity = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user activity for connection {ConnectionId}", connectionId);
            }
        }

        public async Task CleanupInactiveConnectionsAsync()
        {
            try
            {
                // Calculate cutoff time before query
                var cutoffTime = DateTimeOffset.UtcNow.Subtract(_inactiveThreshold);

                var inactiveConnections = await _context.WebsocketConnections
                    .Where(wc => wc.LastActivity < cutoffTime)
                    .ToListAsync();

                if (inactiveConnections.Any())
                {
                    _context.WebsocketConnections.RemoveRange(inactiveConnections);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} inactive connections", inactiveConnections.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up inactive connections");
            }
        }
    }
}
