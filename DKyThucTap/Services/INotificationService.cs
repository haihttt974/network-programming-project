using DKyThucTap.Models.DTOs;

namespace DKyThucTap.Services
{
    public interface INotificationService
    {
        // Basic CRUD operations
        Task<NotificationDto?> GetNotificationByIdAsync(int notificationId, int userId);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createDto);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAsUnreadAsync(int notificationId, int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);

        // Bulk operations
        Task<int> MarkAllAsReadAsync(int userId);
        Task<int> BulkActionAsync(int userId, BulkNotificationActionDto actionDto);
        Task<int> DeleteOldNotificationsAsync(int userId, int daysOld = 30);

        // Notification creation helpers
        Task<NotificationDto> CreateJobApplicationNotificationAsync(int userId, string jobTitle, int applicationId);
        Task<NotificationDto> CreateJobStatusUpdateNotificationAsync(int userId, string jobTitle, string status, int applicationId);
        Task<NotificationDto> CreateNewJobPostingNotificationAsync(int userId, string jobTitle, int positionId);
        Task<NotificationDto> CreateCompanyInvitationNotificationAsync(int userId, string companyName, int companyId);
        Task<NotificationDto> CreateSystemAnnouncementAsync(int userId, string title, string message);
        Task<NotificationDto> CreateMessageNotificationAsync(int userId, string senderName, int messageId);

        // Broadcast notifications
        Task<int> BroadcastToAllUsersAsync(string title, string message, string notificationType);
        Task<int> BroadcastToUsersByRoleAsync(string role, string title, string message, string notificationType);
        Task<int> BroadcastToCompanyUsersAsync(int companyId, string title, string message, string notificationType);

        // Real-time notification support
        Task NotifyUserRealTimeAsync(int userId, NotificationDto notification);
        Task<List<int>> GetOnlineUserIdsAsync();
    }
}
