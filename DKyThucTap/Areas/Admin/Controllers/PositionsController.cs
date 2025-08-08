using DKyThucTap.Data;
using DKyThucTap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DKyThucTap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PositionsController : Controller
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(DKyThucTapContext context, ILogger<PositionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Positions
        public async Task<IActionResult> Index(string search, string company, string status, string type)
        {
            try
            {
                _logger.LogInformation("Loading positions list with filters - search: {Search}, company: {Company}, status: {Status}, type: {Type}",
                    search, company, status, type);

                var positions = _context.Positions
                    .AsNoTracking()
                    .Include(p => p.Company)
                    .Include(p => p.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(p => p.Applications)
                    .Include(p => p.Category)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    positions = positions.Where(p =>
                        p.Title.Contains(search) ||
                        (p.Description != null && p.Description.Contains(search)) ||
                        (p.Company != null && p.Company.Name.Contains(search)));
                }

                // Apply company filter
                if (!string.IsNullOrEmpty(company))
                {
                    positions = positions.Where(p => p.Company.Name.Contains(company));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "active")
                    {
                        positions = positions.Where(p => p.IsActive == true);
                    }
                    else if (status == "inactive")
                    {
                        positions = positions.Where(p => p.IsActive == false);
                    }
                    else if (status == "expired")
                    {
                        positions = positions.Where(p => p.ApplicationDeadline.HasValue &&
                            p.ApplicationDeadline.Value < DateOnly.FromDateTime(DateTime.Now));
                    }
                }

                // Apply type filter
                if (!string.IsNullOrEmpty(type))
                {
                    positions = positions.Where(p => p.PositionType == type);
                }

                // Get filter options
                var companies = await _context.Companies
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .Distinct()
                    .ToListAsync();

                var positionTypes = await _context.Positions
                    .Where(p => !string.IsNullOrEmpty(p.PositionType))
                    .Select(p => p.PositionType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                ViewBag.Companies = companies;
                ViewBag.PositionTypes = positionTypes;
                ViewBag.SelectedCompany = company;
                ViewBag.SelectedStatus = status;
                ViewBag.SelectedType = type;
                ViewBag.Search = search;

                var positionsList = await positions
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} positions", positionsList.Count);

                return View(positionsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading positions list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách vị trí tuyển dụng.";
                return View(new List<Position>());
            }
        }

        // GET: Admin/Positions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null ID");
                return NotFound();
            }

            try
            {
                var position = await _context.Positions
                    .AsNoTracking()
                    .Include(p => p.Company)
                    .Include(p => p.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(p => p.Applications)
                        .ThenInclude(a => a.User)
                            .ThenInclude(u => u.UserProfile)
                    .Include(p => p.Category)
                    .Include(p => p.PositionSkills)
                        .ThenInclude(ps => ps.Skill)
                    .FirstOrDefaultAsync(p => p.PositionId == id);

                if (position == null)
                {
                    _logger.LogWarning("Position not found with ID: {PositionId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Loading details for position ID: {PositionId}", id);
                return View(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading position details for ID: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin vị trí tuyển dụng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Positions/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string adminPassword, string reason)
        {
            _logger.LogInformation("ToggleStatus called for position ID: {PositionId}", id);

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

                // Get target position
                var position = await _context.Positions
                    .Include(p => p.Company)
                    .FirstOrDefaultAsync(p => p.PositionId == id);

                if (position == null)
                {
                    _logger.LogWarning("Target position not found with ID: {PositionId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy vị trí tuyển dụng.";
                    return RedirectToAction(nameof(Index));
                }

                // Toggle position status
                var oldStatus = position.IsActive;
                position.IsActive = !position.IsActive;

                await _context.SaveChangesAsync();

                var statusText = position.IsActive == true ? "kích hoạt" : "vô hiệu hóa";

                _logger.LogInformation("Position {PositionId} status changed from {OldStatus} to {NewStatus} by admin {AdminId}. Reason: {Reason}",
                    id, oldStatus, position.IsActive, currentUserId, reason);

                TempData["SuccessMessage"] = $"Đã {statusText} vị trí '{position.Title}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling position status for position ID: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi trạng thái vị trí tuyển dụng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Positions/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string adminPassword, string reason)
        {
            _logger.LogInformation("Delete called for position ID: {PositionId}", id);

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không được để trống.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "Lý do xóa không được để trống.";
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

                // Get target position with all related data
                var position = await _context.Positions
                    .Include(p => p.Company)
                    .Include(p => p.Applications)
                    .Include(p => p.PositionSkills)
                    .FirstOrDefaultAsync(p => p.PositionId == id);

                if (position == null)
                {
                    _logger.LogWarning("Target position not found with ID: {PositionId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy vị trí tuyển dụng.";
                    return RedirectToAction(nameof(Index));
                }

                var positionTitle = position.Title;
                var companyName = position.Company?.Name ?? "N/A";

                // Delete position (cascade delete will handle related records)
                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Position {PositionId} '{PositionTitle}' from company '{CompanyName}' deleted by admin {AdminId} for reason: {Reason}",
                    id, positionTitle, companyName, currentUserId, reason);

                TempData["SuccessMessage"] = $"Đã xóa vị trí '{positionTitle}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting position with ID: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa vị trí tuyển dụng. Có thể vị trí này có dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Positions/Statistics
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var stats = new PositionStatisticsViewModel
                {
                    TotalPositions = await _context.Positions.CountAsync(),
                    ActivePositions = await _context.Positions.CountAsync(p => p.IsActive == true),
                    ExpiredPositions = await _context.Positions
                        .CountAsync(p => p.ApplicationDeadline.HasValue &&
                                       p.ApplicationDeadline.Value < DateOnly.FromDateTime(DateTime.Now)),
                    TotalApplications = await _context.Applications.CountAsync(),

                    // Positions by type
                    PositionsByType = await _context.Positions
                        .Where(p => !string.IsNullOrEmpty(p.PositionType))
                        .GroupBy(p => p.PositionType)
                        .Select(g => new PositionTypeStatistic
                        {
                            Type = g.Key,
                            Count = g.Count(),
                            ActiveCount = g.Count(p => p.IsActive == true)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToListAsync(),

                    // Top companies by positions
                    TopCompaniesByPositions = await _context.Positions
                        .Include(p => p.Company)
                        .Where(p => p.Company != null)
                        .GroupBy(p => p.Company.Name)
                        .Select(g => new CompanyPositionStatistic
                        {
                            CompanyName = g.Key,
                            TotalPositions = g.Count(),
                            ActivePositions = g.Count(p => p.IsActive == true),
                            TotalApplications = g.Sum(p => p.Applications.Count())
                        })
                        .OrderByDescending(x => x.TotalPositions)
                        .Take(10)
                        .ToListAsync()
                };

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading position statistics");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thống kê vị trí tuyển dụng.";
                return View(new PositionStatisticsViewModel());
            }
        }
    }

    // ViewModels for statistics
    public class PositionStatisticsViewModel
    {
        public int TotalPositions { get; set; }
        public int ActivePositions { get; set; }
        public int ExpiredPositions { get; set; }
        public int TotalApplications { get; set; }
        public List<PositionTypeStatistic> PositionsByType { get; set; } = new List<PositionTypeStatistic>();
        public List<CompanyPositionStatistic> TopCompaniesByPositions { get; set; } = new List<CompanyPositionStatistic>();
    }

    public class PositionTypeStatistic
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public int ActiveCount { get; set; }
    }

    public class CompanyPositionStatistic
    {
        public string CompanyName { get; set; }
        public int TotalPositions { get; set; }
        public int ActivePositions { get; set; }
        public int TotalApplications { get; set; }
    }
}