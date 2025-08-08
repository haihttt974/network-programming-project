using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DKyThucTap.Services;
using DKyThucTap.Models.DTOs;
using DKyThucTap.Models.Enums;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Route("Test/Notification")]
    [Authorize]
    public class TestNotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<TestNotificationController> _logger;

        public TestNotificationController(INotificationService notificationService, ILogger<TestNotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("CreateSample")]
        public async Task<IActionResult> CreateSampleNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var sampleNotifications = new List<CreateNotificationDto>
                {
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Đơn ứng tuyển được chấp nhận",
                        Message = "Chúc mừng! Đơn ứng tuyển của bạn cho vị trí 'Senior Developer' tại công ty ABC đã được chấp nhận.",
                        NotificationType = NotificationTypes.JobStatusUpdate,
                        RelatedEntityType = RelatedEntityTypes.Application,
                        RelatedEntityId = 1
                    },
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Việc làm mới phù hợp",
                        Message = "Có một vị trí 'Frontend Developer' mới tại công ty XYZ phù hợp với hồ sơ của bạn.",
                        NotificationType = NotificationTypes.NewJobPosting,
                        RelatedEntityType = RelatedEntityTypes.Position,
                        RelatedEntityId = 2
                    },
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Lời mời từ công ty",
                        Message = "Công ty DEF muốn mời bạn tham gia phỏng vấn cho vị trí 'Project Manager'.",
                        NotificationType = NotificationTypes.CompanyInvitation,
                        RelatedEntityType = RelatedEntityTypes.Company,
                        RelatedEntityId = 3
                    },
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Tin nhắn mới",
                        Message = "Bạn có tin nhắn mới từ HR Manager của công ty ABC.",
                        NotificationType = NotificationTypes.MessageReceived,
                        RelatedEntityType = RelatedEntityTypes.Message,
                        RelatedEntityId = 4
                    },
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Cập nhật hồ sơ",
                        Message = "Hồ sơ của bạn đã được cập nhật thành công. Hãy kiểm tra lại thông tin.",
                        NotificationType = NotificationTypes.ProfileUpdate,
                        RelatedEntityType = RelatedEntityTypes.User,
                        RelatedEntityId = userId
                    },
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Thông báo bảo mật",
                        Message = "Có một lần đăng nhập mới từ thiết bị khác. Nếu không phải bạn, hãy thay đổi mật khẩu ngay.",
                        NotificationType = NotificationTypes.AccountSecurity,
                        RelatedEntityType = RelatedEntityTypes.User,
                        RelatedEntityId = userId
                    },
                    new CreateNotificationDto
                    {
                        UserId = userId,
                        Title = "Thông báo hệ thống",
                        Message = "Hệ thống sẽ bảo trì từ 2:00 AM đến 4:00 AM ngày mai. Vui lòng lưu ý.",
                        NotificationType = NotificationTypes.SystemAnnouncement,
                        RelatedEntityType = RelatedEntityTypes.System
                    }
                };

                var createdNotifications = new List<NotificationDto>();
                foreach (var notificationDto in sampleNotifications)
                {
                    var created = await _notificationService.CreateNotificationAsync(notificationDto);
                    createdNotifications.Add(created);
                }

                return Json(new
                {
                    success = true,
                    message = $"Đã tạo {createdNotifications.Count} thông báo mẫu",
                    notifications = createdNotifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample notifications");
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpPost("CreateJobApplication")]
        public async Task<IActionResult> CreateJobApplicationNotification()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var notification = await _notificationService.CreateJobApplicationNotificationAsync(
                    userId, "Full Stack Developer", 123);

                return Json(new
                {
                    success = true,
                    message = "Đã tạo thông báo ứng tuyển",
                    notification = notification
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job application notification");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("CreateJobStatusUpdate")]
        public async Task<IActionResult> CreateJobStatusUpdateNotification()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var notification = await _notificationService.CreateJobStatusUpdateNotificationAsync(
                    userId, "Backend Developer", "Đã được chấp nhận", 456);

                return Json(new
                {
                    success = true,
                    message = "Đã tạo thông báo cập nhật trạng thái",
                    notification = notification
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job status update notification");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("CreateSystemAnnouncement")]
        public async Task<IActionResult> CreateSystemAnnouncement()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var notification = await _notificationService.CreateSystemAnnouncementAsync(
                    userId, "Cập nhật hệ thống", "Hệ thống đã được cập nhật với nhiều tính năng mới. Hãy khám phá ngay!");

                return Json(new
                {
                    success = true,
                    message = "Đã tạo thông báo hệ thống",
                    notification = notification
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system announcement");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("BroadcastToAll")]
        public async Task<IActionResult> BroadcastToAll()
        {
            try
            {
                // Only allow admins to broadcast
                if (!User.HasClaim("Permission", "manage_users"))
                {
                    return Forbid();
                }

                var count = await _notificationService.BroadcastToAllUsersAsync(
                    "Thông báo quan trọng",
                    "Đây là thông báo test được gửi đến tất cả người dùng trong hệ thống.",
                    NotificationTypes.SystemAnnouncement);

                return Json(new
                {
                    success = true,
                    message = $"Đã gửi thông báo đến {count} người dùng",
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting to all users");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete("ClearAll")]
        public async Task<IActionResult> ClearAllNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var count = await _notificationService.DeleteOldNotificationsAsync(userId, 0); // Delete all

                return Json(new
                {
                    success = true,
                    message = $"Đã xóa {count} thông báo",
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all notifications");
                return Json(new { success = false, error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
