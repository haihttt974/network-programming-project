using DKyThucTap.Data;
using DKyThucTap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace DKyThucTap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(DKyThucTapContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(string search, string role)
        {
            try
            {
                _logger.LogInformation("Loading users list with search: {Search}, role: {Role}", search, role);

                var users = _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .Include(u => u.UserProfile)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u =>
                        u.Email.Contains(search) ||
                        (u.UserProfile != null && u.UserProfile.FirstName.Contains(search)) ||
                        (u.UserProfile != null && u.UserProfile.LastName.Contains(search)));
                }

                // Apply role filter
                if (!string.IsNullOrEmpty(role))
                {
                    users = users.Where(u => u.Role.RoleName == role);
                }

                // Get roles for filter dropdown
                var roles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
                ViewBag.Roles = roles;
                ViewBag.SelectedRole = role;
                ViewBag.Search = search;

                var usersList = await users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} users", usersList.Count);

                return View(usersList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách người dùng.";
                return View(new List<User>());
            }
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null ID");
                return NotFound();
            }

            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(m => m.UserId == id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Loading details for user ID: {UserId}", id);
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin người dùng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string adminPassword)
        {
            _logger.LogInformation("ToggleActive called for user ID: {UserId}", id);

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không được để trống.";
                    return RedirectToAction(nameof(Index));
                }

                // Get current user ID
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    _logger.LogError("Could not determine current user ID");
                    TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                    return RedirectToAction(nameof(Index));
                }

                // Prevent self-locking
                if (id == currentUserId)
                {
                    _logger.LogWarning("Attempt to toggle own account by user ID: {UserId}", id);
                    TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                    return RedirectToAction(nameof(Index));
                }

                // Get current admin user
                var admin = await _context.Users.FindAsync(currentUserId);
                if (admin == null)
                {
                    _logger.LogError("Admin user not found with ID: {AdminId}", currentUserId);
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản admin.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify admin password
                var authService = HttpContext.RequestServices.GetService<DKyThucTap.Services.IAuthService>();
                if (authService == null)
                {
                    _logger.LogError("AuthService not found in DI container");
                    TempData["ErrorMessage"] = "Lỗi hệ thống: Không thể xác thực mật khẩu.";
                    return RedirectToAction(nameof(Index));
                }

                if (!authService.VerifyPassword(adminPassword, admin.PasswordHash))
                {
                    _logger.LogWarning("Invalid admin password attempt by user ID: {UserId}", currentUserId);
                    TempData["ErrorMessage"] = "Mật khẩu admin không đúng.";
                    return RedirectToAction(nameof(Index));
                }

                // Get target user
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    _logger.LogWarning("Target user not found with ID: {UserId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                // Toggle user status
                var oldStatus = user.IsActive;
                user.IsActive = !user.IsActive;

                await _context.SaveChangesAsync();

                var statusText = user.IsActive == true ? "mở khóa" : "khóa";
                var userName = user.UserProfile != null ?
                    $"{user.UserProfile.FirstName} {user.UserProfile.LastName}" :
                    user.Email;

                _logger.LogInformation("User {UserId} status changed from {OldStatus} to {NewStatus} by admin {AdminId}",
                    id, oldStatus, user.IsActive, currentUserId);

                TempData["SuccessMessage"] = $"Đã {statusText} tài khoản của {userName} thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for user ID: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi trạng thái tài khoản.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}