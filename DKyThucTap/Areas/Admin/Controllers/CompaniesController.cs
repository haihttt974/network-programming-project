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
    public class CompaniesController : Controller
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(DKyThucTapContext context, ILogger<CompaniesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Companies
        public async Task<IActionResult> Index(string search, string industry, string status)
        {
            try
            {
                _logger.LogInformation("Loading companies list with search: {Search}, industry: {Industry}, status: {Status}",
                    search, industry, status);

                var companies = _context.Companies
                    .AsNoTracking()
                    .Include(c => c.CreatedByNavigation)
                    .Include(c => c.Positions)
                    .Include(c => c.CompanyReviews)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    companies = companies.Where(c =>
                        c.Name.Contains(search) ||
                        (c.Description != null && c.Description.Contains(search)) ||
                        (c.Location != null && c.Location.Contains(search)));
                }

                // Apply industry filter
                if (!string.IsNullOrEmpty(industry))
                {
                    companies = companies.Where(c => c.Industry == industry);
                }

                // Apply status filter (using IsActive logic - you may need to add IsActive field to Company model)
                // For now, we'll use a different approach based on whether company has active positions
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "active")
                    {
                        companies = companies.Where(c => c.Positions.Any(p => p.IsActive == true));
                    }
                    else if (status == "inactive")
                    {
                        companies = companies.Where(c => !c.Positions.Any(p => p.IsActive == true));
                    }
                }

                // Get industries for filter dropdown
                var industries = await _context.Companies
                    .Where(c => !string.IsNullOrEmpty(c.Industry))
                    .Select(c => c.Industry)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToListAsync();

                ViewBag.Industries = industries;
                ViewBag.SelectedIndustry = industry;
                ViewBag.SelectedStatus = status;
                ViewBag.Search = search;

                var companiesList = await companies
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} companies", companiesList.Count);

                return View(companiesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách công ty.";
                return View(new List<Company>());
            }
        }

        // GET: Admin/Companies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null ID");
                return NotFound();
            }

            try
            {
                var company = await _context.Companies
                    .AsNoTracking()
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions.Where(p => p.IsActive == true))
                    .Include(c => c.CompanyReviews.Where(r => r.IsApproved == true))
                        .ThenInclude(r => r.User)
                            .ThenInclude(u => u.UserProfile)
                    .FirstOrDefaultAsync(c => c.CompanyId == id);

                if (company == null)
                {
                    _logger.LogWarning("Company not found with ID: {CompanyId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Loading details for company ID: {CompanyId}", id);
                return View(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company details for ID: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin công ty.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Companies/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string adminPassword, string action)
        {
            _logger.LogInformation("ToggleStatus called for company ID: {CompanyId}, action: {Action}", id, action);

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không được để trống.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(action) || !new[] { "approve", "suspend" }.Contains(action))
                {
                    TempData["ErrorMessage"] = "Hành động không hợp lệ.";
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

                // Get target company
                var company = await _context.Companies
                    .Include(c => c.Positions)
                    .FirstOrDefaultAsync(c => c.CompanyId == id);

                if (company == null)
                {
                    _logger.LogWarning("Target company not found with ID: {CompanyId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy công ty.";
                    return RedirectToAction(nameof(Index));
                }

                // Perform action based on request
                string actionText = "";
                if (action == "approve")
                {
                    // Activate all company positions
                    foreach (var position in company.Positions)
                    {
                        position.IsActive = true;
                    }
                    actionText = "duyệt";
                }
                else if (action == "suspend")
                {
                    // Deactivate all company positions
                    foreach (var position in company.Positions)
                    {
                        position.IsActive = false;
                    }
                    actionText = "tạm khóa";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} status changed to {Action} by admin {AdminId}",
                    id, action, currentUserId);

                TempData["SuccessMessage"] = $"Đã {actionText} công ty '{company.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling company status for company ID: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi trạng thái công ty.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Companies/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string adminPassword, string reason)
        {
            _logger.LogInformation("Delete called for company ID: {CompanyId}", id);

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

                // Get target company with all related data
                var company = await _context.Companies
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.CompanyReviews)
                    .FirstOrDefaultAsync(c => c.CompanyId == id);

                if (company == null)
                {
                    _logger.LogWarning("Target company not found with ID: {CompanyId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy công ty.";
                    return RedirectToAction(nameof(Index));
                }

                var companyName = company.Name;

                // Delete company (cascade delete will handle related records)
                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} '{CompanyName}' deleted by admin {AdminId} for reason: {Reason}",
                    id, companyName, currentUserId, reason);

                TempData["SuccessMessage"] = $"Đã xóa công ty '{companyName}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company with ID: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa công ty. Có thể công ty này có dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Companies/ViolationReports
        public async Task<IActionResult> ViolationReports()
        {
            try
            {
                // Get companies with violation reports (negative reviews or low ratings)
                var companiesWithIssues = await _context.Companies
                    .AsNoTracking()
                    .Include(c => c.CompanyReviews.Where(r => r.Rating <= 2 ||
                        (r.Comment != null && (r.Comment.Contains("vi phạm") ||
                                               r.Comment.Contains("lừa đảo") ||
                                               r.Comment.Contains("không trả lương")))))
                        .ThenInclude(r => r.User)
                            .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                    .Where(c => c.CompanyReviews.Any(r => r.Rating <= 2))
                    .OrderByDescending(c => c.CompanyReviews.Count(r => r.Rating <= 2))
                    .ToListAsync();

                ViewBag.Title = "Báo cáo vi phạm công ty";
                return View(companiesWithIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading violation reports");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải báo cáo vi phạm.";
                return View(new List<Company>());
            }
        }
    }
}