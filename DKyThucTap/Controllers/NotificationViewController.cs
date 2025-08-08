using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DKyThucTap.Services;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Authorize]
    public class NotificationViewController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationViewController> _logger;

        public NotificationViewController(INotificationService notificationService, ILogger<NotificationViewController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
                var summary = await _notificationService.GetNotificationSummaryAsync(userId);

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = summary.TotalCount;
                ViewBag.UnreadCount = summary.UnreadCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)summary.TotalCount / pageSize);

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications page for user {UserId}", GetCurrentUserId());
                return View("Error");
            }
        }

        public async Task<IActionResult> Unread(int page = 1, int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var allNotifications = await _notificationService.GetUserNotificationsAsync(userId, 1, 1000); // Get more to filter
                var unreadNotifications = allNotifications.Where(n => !n.IsRead).Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var unreadCount = allNotifications.Count(n => !n.IsRead);

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = unreadCount;
                ViewBag.UnreadCount = unreadCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)unreadCount / pageSize);
                ViewBag.FilterType = "unread";

                return View("Index", unreadNotifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unread notifications for user {UserId}", GetCurrentUserId());
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _notificationService.MarkAsReadAsync(id, userId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã đánh dấu thông báo là đã đọc.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể đánh dấu thông báo. Vui lòng thử lại.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _notificationService.DeleteNotificationAsync(id, userId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã xóa thông báo.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa thông báo. Vui lòng thử lại.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var count = await _notificationService.MarkAllAsReadAsync(userId);
                
                TempData["SuccessMessage"] = $"Đã đánh dấu {count} thông báo là đã đọc.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", GetCurrentUserId());
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOld(int daysOld = 30)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var count = await _notificationService.DeleteOldNotificationsAsync(userId, daysOld);
                
                TempData["SuccessMessage"] = $"Đã xóa {count} thông báo cũ (hơn {daysOld} ngày).";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old notifications for user {UserId}", GetCurrentUserId());
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
