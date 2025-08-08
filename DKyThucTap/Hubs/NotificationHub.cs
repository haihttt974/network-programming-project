using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DKyThucTap.Services;
using DKyThucTap.Models.DTOs;

namespace DKyThucTap.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IOnlineUserService _onlineUserService;
        private readonly ILogger<NotificationHub> _logger;
        private static readonly Dictionary<int, HashSet<string>> _userConnections = new();
        private static readonly object _lock = new object();

        public NotificationHub(IOnlineUserService onlineUserService, ILogger<NotificationHub> logger)
        {
            _onlineUserService = onlineUserService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId > 0)
                {
                    var connectionId = Context.ConnectionId;
                    
                    // Add to our connection tracking
                    lock (_lock)
                    {
                        if (!_userConnections.ContainsKey(userId))
                        {
                            _userConnections[userId] = new HashSet<string>();
                        }
                        _userConnections[userId].Add(connectionId);
                    }

                    // Add to user group for easy broadcasting
                    await Groups.AddToGroupAsync(connectionId, $"User_{userId}");

                    // Update online user service with SignalR connection info
                    var clientInfo = $"SignalR - {Context.GetHttpContext()?.Request.Headers.UserAgent}";
                    await _onlineUserService.AddUserConnectionAsync($"signalr_{connectionId}", userId, clientInfo);

                    _logger.LogInformation("User {UserId} connected to NotificationHub with connection {ConnectionId}", 
                        userId, connectionId);

                    // Send connection confirmation
                    await Clients.Caller.SendAsync("Connected", new { userId, connectionId, timestamp = DateTimeOffset.UtcNow });
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetCurrentUserId();
                var connectionId = Context.ConnectionId;

                if (userId > 0)
                {
                    // Remove from our connection tracking
                    lock (_lock)
                    {
                        if (_userConnections.ContainsKey(userId))
                        {
                            _userConnections[userId].Remove(connectionId);
                            if (_userConnections[userId].Count == 0)
                            {
                                _userConnections.Remove(userId);
                            }
                        }
                    }

                    // Remove from user group
                    await Groups.RemoveFromGroupAsync(connectionId, $"User_{userId}");

                    // Remove from online user service
                    await _onlineUserService.RemoveUserConnectionAsync($"signalr_{connectionId}");

                    _logger.LogInformation("User {UserId} disconnected from NotificationHub with connection {ConnectionId}", 
                        userId, connectionId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        // Client can call this to update their activity
        public async Task UpdateActivity()
        {
            try
            {
                var userId = GetCurrentUserId();
                var connectionId = Context.ConnectionId;

                if (userId > 0)
                {
                    await _onlineUserService.UpdateUserActivityAsync($"signalr_{connectionId}");
                    await Clients.Caller.SendAsync("ActivityUpdated", new { timestamp = DateTimeOffset.UtcNow });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating activity for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        // Client can call this to join specific notification groups (future enhancement)
        public async Task JoinNotificationGroup(string groupName)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Caller.SendAsync("JoinedGroup", new { groupName, timestamp = DateTimeOffset.UtcNow });
                
                _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", 
                    Context.ConnectionId, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining group {GroupName} for connection {ConnectionId}", 
                    groupName, Context.ConnectionId);
            }
        }

        public async Task LeaveNotificationGroup(string groupName)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                await Clients.Caller.SendAsync("LeftGroup", new { groupName, timestamp = DateTimeOffset.UtcNow });
                
                _logger.LogInformation("Connection {ConnectionId} left group {GroupName}", 
                    Context.ConnectionId, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group {GroupName} for connection {ConnectionId}", 
                    groupName, Context.ConnectionId);
            }
        }

        // Static method to get user connections (for use by NotificationService)
        public static List<string> GetUserConnections(int userId)
        {
            lock (_lock)
            {
                return _userConnections.ContainsKey(userId) 
                    ? _userConnections[userId].ToList() 
                    : new List<string>();
            }
        }

        // Static method to get all online user IDs
        public static List<int> GetOnlineUserIds()
        {
            lock (_lock)
            {
                return _userConnections.Keys.ToList();
            }
        }

        public static bool IsUserOnline(int userId)
        {
            lock (_lock)
            {
                return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    // Extension class for IHubContext to send notifications
    public static class NotificationHubExtensions
    {
        public static async Task SendNotificationToUserAsync(this IHubContext<NotificationHub> hubContext, 
            int userId, NotificationDto notification)
        {
            try
            {
                await hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("NewNotification", notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending real-time notification to user {userId}: {ex.Message}");
            }
        }

        public static async Task SendNotificationCountUpdateAsync(this IHubContext<NotificationHub> hubContext, 
            int userId, int newCount)
        {
            try
            {
                await hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("NotificationCountUpdate", new { count = newCount, timestamp = DateTimeOffset.UtcNow });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending count update to user {userId}: {ex.Message}");
            }
        }

        public static async Task BroadcastSystemNotificationAsync(this IHubContext<NotificationHub> hubContext, 
            NotificationDto notification)
        {
            try
            {
                await hubContext.Clients.All.SendAsync("SystemNotification", notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting system notification: {ex.Message}");
            }
        }
    }
}
