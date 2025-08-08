namespace DKyThucTap.Models.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? NotificationType { get; set; }
        public string TimeAgo { get; set; } = null!;
        public string IconClass { get; set; } = null!;
        public string ColorClass { get; set; } = null!;
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? NotificationType { get; set; }
    }

    public class NotificationSummaryDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new();
    }

    public class BulkNotificationActionDto
    {
        public List<int> NotificationIds { get; set; } = new();
        public string Action { get; set; } = null!; // "mark-read", "mark-unread", "delete"
    }
}

namespace DKyThucTap.Models.Enums
{
    public static class NotificationTypes
    {
        public const string JobApplication = "job_application";
        public const string JobStatusUpdate = "job_status_update";
        public const string NewJobPosting = "new_job_posting";
        public const string CompanyInvitation = "company_invitation";
        public const string SystemAnnouncement = "system_announcement";
        public const string ProfileUpdate = "profile_update";
        public const string MessageReceived = "message_received";
        public const string AccountSecurity = "account_security";
        public const string CompanyUpdate = "company_update";
    }

    public static class RelatedEntityTypes
    {
        public const string Application = "Application";
        public const string Position = "Position";
        public const string Company = "Company";
        public const string User = "User";
        public const string Message = "Message";
        public const string System = "System";
    }

    public static class NotificationIcons
    {
        public static string GetIcon(string? notificationType)
        {
            return notificationType switch
            {
                NotificationTypes.JobApplication => "fas fa-briefcase",
                NotificationTypes.JobStatusUpdate => "fas fa-clipboard-check",
                NotificationTypes.NewJobPosting => "fas fa-plus-circle",
                NotificationTypes.CompanyInvitation => "fas fa-building",
                NotificationTypes.SystemAnnouncement => "fas fa-bullhorn",
                NotificationTypes.ProfileUpdate => "fas fa-user-edit",
                NotificationTypes.MessageReceived => "fas fa-envelope",
                NotificationTypes.AccountSecurity => "fas fa-shield-alt",
                _ => "fas fa-bell"
            };
        }

        public static string GetColor(string? notificationType)
        {
            return notificationType switch
            {
                NotificationTypes.JobApplication => "text-primary",
                NotificationTypes.JobStatusUpdate => "text-success",
                NotificationTypes.NewJobPosting => "text-info",
                NotificationTypes.CompanyInvitation => "text-warning",
                NotificationTypes.SystemAnnouncement => "text-danger",
                NotificationTypes.ProfileUpdate => "text-secondary",
                NotificationTypes.MessageReceived => "text-primary",
                NotificationTypes.AccountSecurity => "text-danger",
                _ => "text-muted"
            };
        }
    }
}
