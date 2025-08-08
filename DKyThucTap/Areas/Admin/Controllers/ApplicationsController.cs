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
    public class ApplicationsController : Controller
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(DKyThucTapContext context, ILogger<ApplicationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Applications
        public async Task<IActionResult> Index(string search, string company, string status, string position, int? positionId)
        {
            try
            {
                _logger.LogInformation("Loading applications list with filters - search: {Search}, company: {Company}, status: {Status}, position: {Position}, positionId: {PositionId}",
                    search, company, status, position, positionId);

                var applications = _context.Applications
                    .AsNoTracking()
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Include(a => a.ApplicationStatusHistories.OrderByDescending(h => h.ChangedAt))
                        .ThenInclude(h => h.ChangedByNavigation)
                    .AsQueryable();

                // Filter by specific position if provided
                if (positionId.HasValue)
                {
                    applications = applications.Where(a => a.PositionId == positionId.Value);
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    applications = applications.Where(a =>
                        (a.User.UserProfile != null &&
                         (a.User.UserProfile.FirstName.Contains(search) ||
                          a.User.UserProfile.LastName.Contains(search))) ||
                        a.User.Email.Contains(search) ||
                        a.Position.Title.Contains(search) ||
                        (a.CoverLetter != null && a.CoverLetter.Contains(search)));
                }

                // Apply company filter
                if (!string.IsNullOrEmpty(company))
                {
                    applications = applications.Where(a => a.Position.Company.Name.Contains(company));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    applications = applications.Where(a => a.CurrentStatus == status);
                }

                // Apply position filter
                if (!string.IsNullOrEmpty(position))
                {
                    applications = applications.Where(a => a.Position.Title.Contains(position));
                }

                // Get filter options
                var companies = await _context.Applications
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Where(a => a.Position.Company != null)
                    .Select(a => a.Position.Company.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                var statuses = await _context.Applications
                    .Where(a => !string.IsNullOrEmpty(a.CurrentStatus))
                    .Select(a => a.CurrentStatus)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                var positions = await _context.Applications
                    .Include(a => a.Position)
                    .Select(a => a.Position.Title)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                ViewBag.Companies = companies;
                ViewBag.Statuses = statuses;
                ViewBag.Positions = positions;
                ViewBag.SelectedCompany = company;
                ViewBag.SelectedStatus = status;
                ViewBag.SelectedPosition = position;
                ViewBag.Search = search;
                ViewBag.PositionId = positionId;

                var applicationsList = await applications
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} applications", applicationsList.Count);

                return View(applicationsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading applications list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn ứng tuyển.";
                return View(new List<Application>());
            }
        }

        // GET: Admin/Applications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null ID");
                return NotFound();
            }

            try
            {
                var application = await _context.Applications
                    .AsNoTracking()
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .Include(a => a.User)
                        .ThenInclude(u => u.Role)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Category)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.PositionSkills)
                            .ThenInclude(ps => ps.Skill)
                    .Include(a => a.ApplicationStatusHistories.OrderByDescending(h => h.ChangedAt))
                        .ThenInclude(h => h.ChangedByNavigation)
                            .ThenInclude(u => u.UserProfile)
                    .Include(a => a.ApplicantNotes.OrderByDescending(n => n.CreatedAt))
                        .ThenInclude(n => n.InterviewerUser)
                            .ThenInclude(u => u.UserProfile)
                    .FirstOrDefaultAsync(a => a.ApplicationId == id);

                if (application == null)
                {
                    _logger.LogWarning("Application not found with ID: {ApplicationId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Loading details for application ID: {ApplicationId}", id);
                return View(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading application details for ID: {ApplicationId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn ứng tuyển.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Applications/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, string notes, string adminPassword)
        {
            _logger.LogInformation("UpdateStatus called for application ID: {ApplicationId}, new status: {NewStatus}", id, newStatus);

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không được để trống.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (string.IsNullOrWhiteSpace(newStatus))
                {
                    TempData["ErrorMessage"] = "Trạng thái mới không được để trống.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get current user ID
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    _logger.LogError("Could not determine current user ID");
                    TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get current admin user
                var admin = await _context.Users.FindAsync(currentUserId);
                if (admin == null)
                {
                    _logger.LogError("Admin user not found with ID: {AdminId}", currentUserId);
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản admin.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Verify admin password
                var authService = HttpContext.RequestServices.GetService<DKyThucTap.Services.IAuthService>();
                if (authService == null)
                {
                    _logger.LogError("AuthService not found in DI container");
                    TempData["ErrorMessage"] = "Lỗi hệ thống: Không thể xác thực mật khẩu.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!authService.VerifyPassword(adminPassword, admin.PasswordHash))
                {
                    _logger.LogWarning("Invalid admin password attempt by user ID: {UserId}", currentUserId);
                    TempData["ErrorMessage"] = "Mật khẩu admin không đúng.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get target application
                var application = await _context.Applications
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .FirstOrDefaultAsync(a => a.ApplicationId == id);

                if (application == null)
                {
                    _logger.LogWarning("Target application not found with ID: {ApplicationId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển.";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = application.CurrentStatus;

                // Update application status
                application.CurrentStatus = newStatus;

                // Add status history
                var statusHistory = new ApplicationStatusHistory
                {
                    ApplicationId = id,
                    Status = newStatus,
                    ChangedAt = DateTimeOffset.Now,
                    ChangedBy = currentUserId,
                    Notes = notes
                };

                _context.ApplicationStatusHistories.Add(statusHistory);
                await _context.SaveChangesAsync();

                var applicantName = application.User.UserProfile != null ?
                    $"{application.User.UserProfile.FirstName} {application.User.UserProfile.LastName}" :
                    application.User.Email;

                _logger.LogInformation("Application {ApplicationId} status changed from {OldStatus} to {NewStatus} by admin {AdminId}. Notes: {Notes}",
                    id, oldStatus, newStatus, currentUserId, notes);

                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn ứng tuyển của {applicantName} thành '{newStatus}' thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status for application ID: {ApplicationId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái đơn ứng tuyển.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Applications/AddNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int id, string noteText, string adminPassword)
        {
            _logger.LogInformation("AddNote called for application ID: {ApplicationId}", id);

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không được để trống.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (string.IsNullOrWhiteSpace(noteText))
                {
                    TempData["ErrorMessage"] = "Nội dung ghi chú không được để trống.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get current user ID
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    _logger.LogError("Could not determine current user ID");
                    TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get current admin user
                var admin = await _context.Users.FindAsync(currentUserId);
                if (admin == null)
                {
                    _logger.LogError("Admin user not found with ID: {AdminId}", currentUserId);
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản admin.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Verify admin password
                var authService = HttpContext.RequestServices.GetService<DKyThucTap.Services.IAuthService>();
                if (authService == null || !authService.VerifyPassword(adminPassword, admin.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không đúng.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Check if application exists
                var applicationExists = await _context.Applications.AnyAsync(a => a.ApplicationId == id);
                if (!applicationExists)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển.";
                    return RedirectToAction(nameof(Index));
                }

                // Add note
                var note = new ApplicantNote
                {
                    ApplicationId = id,
                    InterviewerUserId = currentUserId,
                    NoteText = noteText,
                    CreatedAt = DateTimeOffset.Now
                };

                _context.ApplicantNotes.Add(note);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Note added to application {ApplicationId} by admin {AdminId}", id, currentUserId);

                TempData["SuccessMessage"] = "Đã thêm ghi chú thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note for application ID: {ApplicationId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm ghi chú.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Applications/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string adminPassword, string reason)
        {
            _logger.LogInformation("Delete called for application ID: {ApplicationId}", id);

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

                // Get current user ID and verify admin
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                    return RedirectToAction(nameof(Index));
                }

                var admin = await _context.Users.FindAsync(currentUserId);
                if (admin == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản admin.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify admin password
                var authService = HttpContext.RequestServices.GetService<DKyThucTap.Services.IAuthService>();
                if (authService == null || !authService.VerifyPassword(adminPassword, admin.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Mật khẩu admin không đúng.";
                    return RedirectToAction(nameof(Index));
                }

                // Get target application
                var application = await _context.Applications
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Include(a => a.ApplicationStatusHistories)
                    .Include(a => a.ApplicantNotes)
                    .FirstOrDefaultAsync(a => a.ApplicationId == id);

                if (application == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển.";
                    return RedirectToAction(nameof(Index));
                }

                var applicantName = application.User.UserProfile != null ?
                    $"{application.User.UserProfile.FirstName} {application.User.UserProfile.LastName}" :
                    application.User.Email;
                var positionTitle = application.Position.Title;
                var companyName = application.Position.Company?.Name ?? "N/A";

                // Delete application (cascade delete will handle related records)
                _context.Applications.Remove(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {ApplicationId} of {ApplicantName} for position '{PositionTitle}' at company '{CompanyName}' deleted by admin {AdminId} for reason: {Reason}",
                    id, applicantName, positionTitle, companyName, currentUserId, reason);

                TempData["SuccessMessage"] = $"Đã xóa đơn ứng tuyển của {applicantName} thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application with ID: {ApplicationId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa đơn ứng tuyển.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Applications/Statistics
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var stats = new ApplicationStatisticsViewModel
                {
                    TotalApplications = await _context.Applications.CountAsync(),
                    PendingApplications = await _context.Applications.CountAsync(a => a.CurrentStatus == "applied"),
                    ApprovedApplications = await _context.Applications.CountAsync(a => a.CurrentStatus == "approved"),
                    RejectedApplications = await _context.Applications.CountAsync(a => a.CurrentStatus == "rejected"),

                    // Applications by status
                    ApplicationsByStatus = await _context.Applications
                        .Where(a => !string.IsNullOrEmpty(a.CurrentStatus))
                        .GroupBy(a => a.CurrentStatus)
                        .Select(g => new ApplicationStatusStatistic
                        {
                            Status = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToListAsync(),

                    // Applications by month (last 6 months)
                    ApplicationsByMonth = await _context.Applications
                        .Where(a => a.AppliedAt >= DateTimeOffset.Now.AddMonths(-6))
                        .GroupBy(a => new { Year = a.AppliedAt.Value.Year, Month = a.AppliedAt.Value.Month })
                        .Select(g => new ApplicationMonthlyStatistic
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Year).ThenBy(x => x.Month)
                        .ToListAsync(),

                    // Top positions by applications
                    TopPositionsByApplications = await _context.Applications
                        .Include(a => a.Position)
                            .ThenInclude(p => p.Company)
                        .GroupBy(a => new { a.Position.Title, CompanyName = a.Position.Company.Name })
                        .Select(g => new PositionApplicationStatistic
                        {
                            PositionTitle = g.Key.Title,
                            CompanyName = g.Key.CompanyName,
                            ApplicationCount = g.Count(),
                            ApprovedCount = g.Count(a => a.CurrentStatus == "approved"),
                            PendingCount = g.Count(a => a.CurrentStatus == "applied")
                        })
                        .OrderByDescending(x => x.ApplicationCount)
                        .Take(10)
                        .ToListAsync()
                };

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading application statistics");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thống kê đơn ứng tuyển.";
                return View(new ApplicationStatisticsViewModel());
            }
        }
    }

    // ViewModels for statistics
    public class ApplicationStatisticsViewModel
    {
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public List<ApplicationStatusStatistic> ApplicationsByStatus { get; set; } = new List<ApplicationStatusStatistic>();
        public List<ApplicationMonthlyStatistic> ApplicationsByMonth { get; set; } = new List<ApplicationMonthlyStatistic>();
        public List<PositionApplicationStatistic> TopPositionsByApplications { get; set; } = new List<PositionApplicationStatistic>();
    }

    public class ApplicationStatusStatistic
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class ApplicationMonthlyStatistic
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
    }

    public class PositionApplicationStatistic
    {
        public string PositionTitle { get; set; }
        public string CompanyName { get; set; }
        public int ApplicationCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
    }
}