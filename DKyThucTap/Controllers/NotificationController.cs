using Microsoft.AspNetCore.Mvc;
using DKyThucTap.Services;
using DKyThucTap.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DKyThucTap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
                
                return Ok(new { 
                    notifications = notifications,
                    page = page,
                    pageSize = pageSize,
                    timestamp = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetNotificationSummary()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var summary = await _notificationService.GetNotificationSummaryAsync(userId);
                
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification summary");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var count = await _notificationService.GetUnreadCountAsync(userId);
                
                return Ok(new { 
                    count = count,
                    timestamp = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var notification = await _notificationService.GetNotificationByIdAsync(id, userId);
                
                if (notification == null)
                    return NotFound(new { error = "Notification not found" });

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {NotificationId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _notificationService.MarkAsReadAsync(id, userId);
                
                if (!success)
                    return NotFound(new { error = "Notification not found" });

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("mark-unread/{id}")]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _notificationService.MarkAsUnreadAsync(id, userId);
                
                if (!success)
                    return NotFound(new { error = "Notification not found" });

                return Ok(new { message = "Notification marked as unread" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as unread", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var count = await _notificationService.MarkAllAsReadAsync(userId);
                
                return Ok(new { 
                    message = $"Marked {count} notifications as read",
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _notificationService.DeleteNotificationAsync(id, userId);
                
                if (!success)
                    return NotFound(new { error = "Notification not found" });

                return Ok(new { message = "Notification deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("bulk-action")]
        public async Task<IActionResult> BulkAction([FromBody] BulkNotificationActionDto actionDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                if (actionDto.NotificationIds == null || !actionDto.NotificationIds.Any())
                    return BadRequest(new { error = "No notification IDs provided" });

                var count = await _notificationService.BulkActionAsync(userId, actionDto);
                
                return Ok(new { 
                    message = $"Performed {actionDto.Action} on {count} notifications",
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action {Action}", actionDto?.Action);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("old")]
        public async Task<IActionResult> DeleteOldNotifications([FromQuery] int daysOld = 30)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var count = await _notificationService.DeleteOldNotificationsAsync(userId, daysOld);
                
                return Ok(new { 
                    message = $"Deleted {count} old notifications",
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0) return Unauthorized();

                // Only allow creating notifications for yourself or if you're an admin
                var userRole = User.FindFirst("Role")?.Value;
                if (createDto.UserId != currentUserId && userRole != "Admin")
                    return Forbid();

                var notification = await _notificationService.CreateNotificationAsync(createDto);
                
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
