using DKyThucTap.Models.Enums;

namespace DKyThucTap.Services
{
    public interface INotificationIntegrationService
    {
        // Job Application Integration
        Task NotifyJobApplicationSubmittedAsync(int userId, string jobTitle, int applicationId);
        Task NotifyJobApplicationStatusChangedAsync(int userId, string jobTitle, string newStatus, int applicationId);
        Task NotifyJobApplicationRejectedAsync(int userId, string jobTitle, string reason, int applicationId);
        Task NotifyJobApplicationAcceptedAsync(int userId, string jobTitle, string nextSteps, int applicationId);
        Task NotifyJobApplicationRecruiterSubmittedAsync(int recruiterId, string jobTitle, int applicationId);

        // Job Posting Integration
        Task NotifyNewJobMatchingCriteriaAsync(int userId, string jobTitle, int positionId, string matchReason);
        Task NotifyJobPostingExpiredAsync(int recruiterId, string jobTitle, int positionId);
        Task NotifyJobPostingApprovedAsync(int recruiterId, string jobTitle, int positionId);

        // Company Integration
        Task NotifyCompanyInvitationAsync(int userId, string companyName, int companyId, string position);
        Task NotifyCompanyProfileUpdatedAsync(int recruiterId, string companyName, int companyId);
        Task NotifyNewCompanyFollowerAsync(int recruiterId, string followerName, int companyId);
        Task NotifyCompanyRegistrationAsync(int adminId, string companyName, int companyId, string message);

        // User Profile Integration
        Task NotifyProfileCompletionReminderAsync(int userId, int completionPercentage);
        Task NotifyProfileViewedAsync(int userId, string viewerName, string viewerCompany);
        Task NotifySkillEndorsementAsync(int userId, string endorserName, string skillName);

        // System Integration
        Task NotifySystemMaintenanceAsync(int userId, DateTime maintenanceStart, DateTime maintenanceEnd);
        Task NotifySecurityAlertAsync(int userId, string alertType, string details);
        Task NotifyAccountVerificationAsync(int userId, string verificationType);

        // Message Integration
        Task NotifyNewMessageAsync(int userId, string senderName, string messagePreview, int messageId);
        Task NotifyMessageReplyAsync(int userId, string replierName, string messagePreview, int messageId);

        // Bulk Notifications
        Task NotifyAllUsersSystemUpdateAsync(string updateTitle, string updateDetails);
        Task NotifyRecruitersByCompanyAsync(int companyId, string title, string message);
        Task NotifyCandidatesBySkillAsync(string skillName, string title, string message);
    }

    public class NotificationIntegrationService : INotificationIntegrationService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationIntegrationService> _logger;

        public NotificationIntegrationService(INotificationService notificationService, ILogger<NotificationIntegrationService> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // Job Application Integration
        public async Task NotifyJobApplicationSubmittedAsync(int userId, string jobTitle, int applicationId)
        {
            try
            {
                await _notificationService.CreateJobApplicationNotificationAsync(userId, jobTitle, applicationId);
                _logger.LogInformation("Sent job application notification to user {UserId} for job {JobTitle}", userId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job application notification to user {UserId}", userId);
            }
        }

        public async Task NotifyJobApplicationStatusChangedAsync(int userId, string jobTitle, string newStatus, int applicationId)
        {
            try
            {
                await _notificationService.CreateJobStatusUpdateNotificationAsync(userId, jobTitle, newStatus, applicationId);
                _logger.LogInformation("Sent status update notification to user {UserId} for job {JobTitle}: {Status}", userId, jobTitle, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status update notification to user {UserId}", userId);
            }
        }

        public async Task NotifyJobApplicationRejectedAsync(int userId, string jobTitle, string reason, int applicationId)
        {
            try
            {
                var message = $"Rất tiếc, đơn ứng tuyển của bạn cho vị trí '{jobTitle}' không được chấp nhận. Lý do: {reason}";
                await _notificationService.CreateJobStatusUpdateNotificationAsync(userId, jobTitle, "Không được chấp nhận", applicationId);
                _logger.LogInformation("Sent rejection notification to user {UserId} for job {JobTitle}", userId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection notification to user {UserId}", userId);
            }
        }

        public async Task NotifyJobApplicationAcceptedAsync(int userId, string jobTitle, string nextSteps, int applicationId)
        {
            try
            {
                var message = $"Chúc mừng! Đơn ứng tuyển của bạn cho vị trí '{jobTitle}' đã được chấp nhận. Bước tiếp theo: {nextSteps}";
                await _notificationService.CreateJobStatusUpdateNotificationAsync(userId, jobTitle, "Được chấp nhận", applicationId);
                _logger.LogInformation("Sent acceptance notification to user {UserId} for job {JobTitle}", userId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send acceptance notification to user {UserId}", userId);
            }
        }

        // Job Posting Integration
        public async Task NotifyNewJobMatchingCriteriaAsync(int userId, string jobTitle, int positionId, string matchReason)
        {
            try
            {
                await _notificationService.CreateNewJobPostingNotificationAsync(userId, jobTitle, positionId);
                _logger.LogInformation("Sent job matching notification to user {UserId} for job {JobTitle}", userId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job matching notification to user {UserId}", userId);
            }
        }

        public async Task NotifyJobPostingExpiredAsync(int recruiterId, string jobTitle, int positionId)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(new Models.DTOs.CreateNotificationDto
                {
                    UserId = recruiterId,
                    Title = "Tin tuyển dụng hết hạn",
                    Message = $"Tin tuyển dụng '{jobTitle}' của bạn đã hết hạn. Hãy gia hạn hoặc đăng tin mới.",
                    NotificationType = NotificationTypes.JobStatusUpdate,
                    RelatedEntityType = RelatedEntityTypes.Position,
                    RelatedEntityId = positionId
                });
                _logger.LogInformation("Sent job expiration notification to recruiter {RecruiterId} for job {JobTitle}", recruiterId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job expiration notification to recruiter {RecruiterId}", recruiterId);
            }
        }

        public async Task NotifyJobPostingApprovedAsync(int recruiterId, string jobTitle, int positionId)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(new Models.DTOs.CreateNotificationDto
                {
                    UserId = recruiterId,
                    Title = "Tin tuyển dụng được duyệt",
                    Message = $"Tin tuyển dụng '{jobTitle}' của bạn đã được duyệt và hiển thị công khai.",
                    NotificationType = NotificationTypes.JobStatusUpdate,
                    RelatedEntityType = RelatedEntityTypes.Position,
                    RelatedEntityId = positionId
                });
                _logger.LogInformation("Sent job approval notification to recruiter {RecruiterId} for job {JobTitle}", recruiterId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job approval notification to recruiter {RecruiterId}", recruiterId);
            }
        }

        // Company Integration
        public async Task NotifyCompanyInvitationAsync(int userId, string companyName, int companyId, string position)
        {
            try
            {
                await _notificationService.CreateCompanyInvitationNotificationAsync(userId, companyName, companyId);
                _logger.LogInformation("Sent company invitation notification to user {UserId} from company {CompanyName}", userId, companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send company invitation notification to user {UserId}", userId);
            }
        }

        public async Task NotifyCompanyProfileUpdatedAsync(int recruiterId, string companyName, int companyId)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(new Models.DTOs.CreateNotificationDto
                {
                    UserId = recruiterId,
                    Title = "Hồ sơ công ty được cập nhật",
                    Message = $"Hồ sơ công ty '{companyName}' đã được cập nhật thành công.",
                    NotificationType = NotificationTypes.ProfileUpdate,
                    RelatedEntityType = RelatedEntityTypes.Company,
                    RelatedEntityId = companyId
                });
                _logger.LogInformation("Sent company profile update notification to recruiter {RecruiterId}", recruiterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send company profile update notification to recruiter {RecruiterId}", recruiterId);
            }
        }

        // System Integration
        public async Task NotifySystemMaintenanceAsync(int userId, DateTime maintenanceStart, DateTime maintenanceEnd)
        {
            try
            {
                var message = $"Hệ thống sẽ bảo trì từ {maintenanceStart:dd/MM/yyyy HH:mm} đến {maintenanceEnd:dd/MM/yyyy HH:mm}. Vui lòng lưu ý.";
                await _notificationService.CreateSystemAnnouncementAsync(userId, "Thông báo bảo trì hệ thống", message);
                _logger.LogInformation("Sent maintenance notification to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send maintenance notification to user {UserId}", userId);
            }
        }

        public async Task NotifySecurityAlertAsync(int userId, string alertType, string details)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(new Models.DTOs.CreateNotificationDto
                {
                    UserId = userId,
                    Title = $"Cảnh báo bảo mật: {alertType}",
                    Message = details,
                    NotificationType = NotificationTypes.AccountSecurity,
                    RelatedEntityType = RelatedEntityTypes.User,
                    RelatedEntityId = userId
                });
                _logger.LogInformation("Sent security alert notification to user {UserId}: {AlertType}", userId, alertType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security alert notification to user {UserId}", userId);
            }
        }

        // Message Integration
        public async Task NotifyNewMessageAsync(int userId, string senderName, string messagePreview, int messageId)
        {
            try
            {
                await _notificationService.CreateMessageNotificationAsync(userId, senderName, messageId);
                _logger.LogInformation("Sent new message notification to user {UserId} from {SenderName}", userId, senderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new message notification to user {UserId}", userId);
            }
        }

        // Bulk Notifications
        public async Task NotifyAllUsersSystemUpdateAsync(string updateTitle, string updateDetails)
        {
            try
            {
                var count = await _notificationService.BroadcastToAllUsersAsync(updateTitle, updateDetails, NotificationTypes.SystemAnnouncement);
                _logger.LogInformation("Broadcasted system update notification to {Count} users", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast system update notification");
            }
        }

        public async Task NotifyCompanyRegistrationAsync(int adminId, string companyName, int companyId, string message)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(new Models.DTOs.CreateNotificationDto
                {
                    UserId = adminId,
                    Title = "Công ty mới đăng ký",
                    Message = $"Công ty '{companyName}' vừa đăng ký. {message}",
                    NotificationType = NotificationTypes.CompanyUpdate,
                    RelatedEntityType = RelatedEntityTypes.Company,
                    RelatedEntityId = companyId
                });
                _logger.LogInformation("Sent company registration notification to admin {AdminId} for company {CompanyName}", adminId, companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send company registration notification to admin {AdminId}", adminId);
            }
        }

        public async Task NotifyJobApplicationRecruiterSubmittedAsync(int recruiterId, string jobTitle, int applicationId)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(new Models.DTOs.CreateNotificationDto
                {
                    UserId = recruiterId,
                    Title = "Đơn ứng tuyển mới",
                    Message = $"Có ứng viên mới ứng tuyển vào vị trí '{jobTitle}'. Hãy xem xét hồ sơ ngay!",
                    NotificationType = NotificationTypes.JobApplication,
                    RelatedEntityType = RelatedEntityTypes.Application,
                    RelatedEntityId = applicationId
                });
                _logger.LogInformation("Sent new job application notification to recruiter {RecruiterId} for job {JobTitle}", recruiterId, jobTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job application submitted notification to recruiter {RecruiterId}", recruiterId);
            }
        }

        // Placeholder implementations for other methods
        public async Task NotifyNewCompanyFollowerAsync(int recruiterId, string followerName, int companyId) => await Task.CompletedTask;
        public async Task NotifyProfileCompletionReminderAsync(int userId, int completionPercentage) => await Task.CompletedTask;
        public async Task NotifyProfileViewedAsync(int userId, string viewerName, string viewerCompany) => await Task.CompletedTask;
        public async Task NotifySkillEndorsementAsync(int userId, string endorserName, string skillName) => await Task.CompletedTask;
        public async Task NotifyAccountVerificationAsync(int userId, string verificationType) => await Task.CompletedTask;
        public async Task NotifyMessageReplyAsync(int userId, string replierName, string messagePreview, int messageId) => await Task.CompletedTask;
        public async Task NotifyRecruitersByCompanyAsync(int companyId, string title, string message) => await Task.CompletedTask;
        public async Task NotifyCandidatesBySkillAsync(string skillName, string title, string message) => await Task.CompletedTask;
    }
}
