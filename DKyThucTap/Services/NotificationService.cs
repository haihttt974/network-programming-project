using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Models.DTOs;
using DKyThucTap.Models.Enums;
using DKyThucTap.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace DKyThucTap.Services
{
    public class NotificationService : INotificationService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IOnlineUserService _onlineUserService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            DKyThucTapContext context,
            ILogger<NotificationService> logger,
            IOnlineUserService onlineUserService,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _onlineUserService = onlineUserService;
            _hubContext = hubContext;
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.NotificationId == notificationId && n.UserId == userId)
                    .FirstOrDefaultAsync();

                return notification != null ? MapToDto(notification) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {NotificationId} for user {UserId}", notificationId, userId);
                return null;
            }
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return notifications.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return new List<NotificationDto>();
            }
        }

        public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(int userId)
        {
            try
            {
                var totalCount = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .CountAsync();

                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .CountAsync();

                var recentNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                return new NotificationSummaryDto
                {
                    TotalCount = totalCount,
                    UnreadCount = unreadCount,
                    RecentNotifications = recentNotifications.Select(MapToDto).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification summary for user {UserId}", userId);
                return new NotificationSummaryDto();
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createDto)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = createDto.UserId,
                    Title = createDto.Title,
                    Message = createDto.Message,
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    RelatedEntityType = createDto.RelatedEntityType,
                    RelatedEntityId = createDto.RelatedEntityId,
                    NotificationType = createDto.NotificationType
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var notificationDto = MapToDto(notification);

                // Send real-time notification if user is online
                await NotifyUserRealTimeAsync(createDto.UserId, notificationDto);

                _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                    notification.NotificationId, createDto.UserId);

                return notificationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", createDto.UserId);
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.NotificationId == notificationId && n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (notification == null) return false;

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as read for user {UserId}", 
                    notificationId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", 
                    notificationId, userId);
                return false;
            }
        }

        public async Task<bool> MarkAsUnreadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.NotificationId == notificationId && n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (notification == null) return false;

                notification.IsRead = false;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as unread for user {UserId}", 
                    notificationId, userId);
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.NotificationId == notificationId && n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (notification == null) return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}", 
                    notificationId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", 
                    notificationId, userId);
                return false;
            }
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                // Send real-time count update
                try
                {
                    var newCount = await GetUnreadCountAsync(userId);
                    await _hubContext.SendNotificationCountUpdateAsync(userId, newCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending real-time count update after mark all as read for user {UserId}", userId);
                }

                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                    unreadNotifications.Count, userId);

                return unreadNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return 0;
            }
        }

        private NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead ?? false,
                CreatedAt = notification.CreatedAt ?? DateTimeOffset.UtcNow,
                RelatedEntityType = notification.RelatedEntityType,
                RelatedEntityId = notification.RelatedEntityId,
                NotificationType = notification.NotificationType,
                TimeAgo = GetTimeAgo(notification.CreatedAt ?? DateTimeOffset.UtcNow),
                IconClass = NotificationIcons.GetIcon(notification.NotificationType),
                ColorClass = NotificationIcons.GetColor(notification.NotificationType)
            };
        }

        private string GetTimeAgo(DateTimeOffset createdAt)
        {
            var timeSpan = DateTimeOffset.UtcNow - createdAt;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";
            
            return createdAt.ToString("dd/MM/yyyy");
        }

        public async Task<int> BulkActionAsync(int userId, BulkNotificationActionDto actionDto)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && actionDto.NotificationIds.Contains(n.NotificationId))
                    .ToListAsync();

                int affectedCount = 0;

                switch (actionDto.Action.ToLower())
                {
                    case "mark-read":
                        foreach (var notification in notifications)
                        {
                            if (notification.IsRead == false)
                            {
                                notification.IsRead = true;
                                affectedCount++;
                            }
                        }
                        break;

                    case "mark-unread":
                        foreach (var notification in notifications)
                        {
                            if (notification.IsRead == true)
                            {
                                notification.IsRead = false;
                                affectedCount++;
                            }
                        }
                        break;

                    case "delete":
                        _context.Notifications.RemoveRange(notifications);
                        affectedCount = notifications.Count;
                        break;
                }

                await _context.SaveChangesAsync();
                return affectedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action {Action} for user {UserId}", actionDto.Action, userId);
                return 0;
            }
        }

        public async Task<int> DeleteOldNotificationsAsync(int userId, int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysOld);
                var oldNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();

                return oldNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old notifications for user {UserId}", userId);
                return 0;
            }
        }

        // Notification creation helpers
        public async Task<NotificationDto> CreateJobApplicationNotificationAsync(int userId, string jobTitle, int applicationId)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = "Đơn ứng tuyển mới",
                Message = $"Bạn đã nộp đơn ứng tuyển cho vị trí: {jobTitle}",
                NotificationType = NotificationTypes.JobApplication,
                RelatedEntityType = RelatedEntityTypes.Application,
                RelatedEntityId = applicationId
            };

            return await CreateNotificationAsync(createDto);
        }

        public async Task<NotificationDto> CreateJobStatusUpdateNotificationAsync(int userId, string jobTitle, string status, int applicationId)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = "Cập nhật trạng thái ứng tuyển",
                Message = $"Trạng thái ứng tuyển cho vị trí '{jobTitle}' đã được cập nhật: {status}",
                NotificationType = NotificationTypes.JobStatusUpdate,
                RelatedEntityType = RelatedEntityTypes.Application,
                RelatedEntityId = applicationId
            };

            return await CreateNotificationAsync(createDto);
        }

        public async Task<NotificationDto> CreateNewJobPostingNotificationAsync(int userId, string jobTitle, int positionId)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = "Việc làm mới phù hợp",
                Message = $"Có việc làm mới phù hợp với bạn: {jobTitle}",
                NotificationType = NotificationTypes.NewJobPosting,
                RelatedEntityType = RelatedEntityTypes.Position,
                RelatedEntityId = positionId
            };

            return await CreateNotificationAsync(createDto);
        }

        public async Task<NotificationDto> CreateCompanyInvitationNotificationAsync(int userId, string companyName, int companyId)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = "Lời mời từ công ty",
                Message = $"Bạn có lời mời làm việc từ công ty: {companyName}",
                NotificationType = NotificationTypes.CompanyInvitation,
                RelatedEntityType = RelatedEntityTypes.Company,
                RelatedEntityId = companyId
            };

            return await CreateNotificationAsync(createDto);
        }

        public async Task<NotificationDto> CreateSystemAnnouncementAsync(int userId, string title, string message)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = NotificationTypes.SystemAnnouncement,
                RelatedEntityType = RelatedEntityTypes.System
            };

            return await CreateNotificationAsync(createDto);
        }

        public async Task<NotificationDto> CreateMessageNotificationAsync(int userId, string senderName, int messageId)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = "Tin nhắn mới",
                Message = $"Bạn có tin nhắn mới từ {senderName}",
                NotificationType = NotificationTypes.MessageReceived,
                RelatedEntityType = RelatedEntityTypes.Message,
                RelatedEntityId = messageId
            };

            return await CreateNotificationAsync(createDto);
        }

        public async Task<int> BroadcastToAllUsersAsync(string title, string message, string notificationType)
        {
            try
            {
                var userIds = await _context.Users.Select(u => u.UserId).ToListAsync();
                int createdCount = 0;

                var sampleNotification = MapToDto(new Notification
                {
                    NotificationId = 0,
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsRead = false
                });

                try
                {
                    await _hubContext.BroadcastSystemNotificationAsync(sampleNotification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting system notification via SignalR");
                }

                foreach (var userId in userIds)
                {
                    var createDto = new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        RelatedEntityType = RelatedEntityTypes.System
                    };

                    await CreateNotificationAsync(createDto);
                    createdCount++;
                }

                _logger.LogInformation("Broadcasted notification to {Count} users", createdCount);
                return createdCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to all users");
                return 0;
            }
        }

        public async Task<int> BroadcastToUsersByRoleAsync(string role, string title, string message, string notificationType)
        {
            try
            {
                var userIds = await _context.Users
                    .Where(u => u.Role.RoleName == role)
                    .Select(u => u.UserId)
                    .ToListAsync();

                int createdCount = 0;

                foreach (var userId in userIds)
                {
                    var createDto = new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        RelatedEntityType = RelatedEntityTypes.System
                    };

                    await CreateNotificationAsync(createDto);
                    createdCount++;
                }

                _logger.LogInformation("Broadcasted notification to {Count} users with role {Role}", createdCount, role);
                return createdCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to users with role {Role}", role);
                return 0;
            }
        }

        public async Task<int> BroadcastToCompanyUsersAsync(int companyId, string title, string message, string notificationType)
        {
            try
            {
                var userIds = await _context.CompanyRecruiters
                    .Where(cr => cr.CompanyId == companyId)
                    .Select(cr => cr.UserId)
                    .ToListAsync();

                int createdCount = 0;

                foreach (var userId in userIds)
                {
                    var createDto = new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        RelatedEntityType = RelatedEntityTypes.Company,
                        RelatedEntityId = companyId
                    };

                    await CreateNotificationAsync(createDto);
                    createdCount++;
                }

                _logger.LogInformation("Broadcasted notification to {Count} users in company {CompanyId}", createdCount, companyId);
                return createdCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to company {CompanyId} users", companyId);
                return 0;
            }
        }

        // Real-time notification support
        public async Task NotifyUserRealTimeAsync(int userId, NotificationDto notification)
        {
            try
            {
                // Check if user is online via SignalR
                if (NotificationHub.IsUserOnline(userId))
                {
                    // Send real-time notification via SignalR
                    await _hubContext.SendNotificationToUserAsync(userId, notification);

                    var newCount = await GetUnreadCountAsync(userId);
                    await _hubContext.SendNotificationCountUpdateAsync(userId, newCount);

                    _logger.LogInformation("Sent real-time notification to online user {UserId}: {Title}",
                        userId, notification.Title);
                }
                else
                {
                    _logger.LogDebug("User {UserId} is offline, skipping real-time notification: {Title}",
                        userId, notification.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending real-time notification to user {UserId}: {Title}",
                    userId, notification.Title);
            }
        }

        public async Task<List<int>> GetOnlineUserIdsAsync()
        {
            try
            {
                var onlineUsers = await _onlineUserService.GetOnlineUsersAsync();
                return onlineUsers.Select(u => u.UserId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online user IDs");
                return new List<int>();
            }
        }
    }
}
