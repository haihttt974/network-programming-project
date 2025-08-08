using DKyThucTap.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKyThucTap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        private readonly ILogger<AdminHomeController> _logger;
        private readonly DKyThucTapContext _context;

        public AdminHomeController(ILogger<AdminHomeController> logger, DKyThucTapContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: AdminHome/AdminDashboard
        public async Task<IActionResult> AdminDashboard()
        {
            _logger.LogInformation("Loading Admin dashboard");

            try
            {
                // Fetch statistics
                var dashboardViewModel = new DashboardViewModel
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalCompanies = await _context.Companies.CountAsync(),
                    ActiveCompanies = await _context.Companies
                        .Where(c => c.Positions.Any(p => p.IsActive == true))
                        .CountAsync(),
                    NewReviews = await _context.CompanyReviews
                        .Where(r => r.CreatedAt >= DateTimeOffset.Now.AddDays(-30))
                        .CountAsync(),
                    ViolationReports = await _context.CompanyReviews
                        .Where(r => r.Rating <= 2)
                        .Select(r => r.CompanyId)
                        .Distinct()
                        .CountAsync()
                };

                // Data for user growth chart (last 6 months)
                var userGrowthData = await _context.Users
                    .Where(u => u.CreatedAt.HasValue && u.CreatedAt >= DateTimeOffset.Now.AddMonths(-6))
                    .GroupBy(u => new { Year = u.CreatedAt.Value.Year, Month = u.CreatedAt.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new UserGrowthPoint
                    {
                        Month = g.Key.Month,
                        Year = g.Key.Year,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Ensure at least 6 months of data for the chart (fill with zeros if needed)
                var lastSixMonths = Enumerable.Range(0, 6)
                    .Select(i => DateTimeOffset.Now.AddMonths(-i))
                    .OrderBy(d => d.Year).ThenBy(d => d.Month)
                    .Select(d => new { d.Year, d.Month })
                    .ToList();

                dashboardViewModel.UserGrowthData = lastSixMonths
                    .GroupJoin(userGrowthData,
                        month => new { month.Year, month.Month },
                        data => new { data.Year, data.Month },
                        (month, data) => new UserGrowthPoint
                        {
                            Year = month.Year,
                            Month = month.Month,
                            Count = data.FirstOrDefault()?.Count ?? 0
                        })
                    .OrderBy(d => d.Year).ThenBy(d => d.Month)
                    .ToList();

                _logger.LogInformation("Dashboard data loaded successfully: {TotalUsers} users, {TotalCompanies} companies",
                    dashboardViewModel.TotalUsers, dashboardViewModel.TotalCompanies);

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");

                // Return a basic dashboard with error message
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard. Vui lòng thử lại.";
                return View(new DashboardViewModel());
            }
        }

        // GET: AdminHome/Index (redirect to AdminDashboard)
        public IActionResult Index()
        {
            return RedirectToAction(nameof(AdminDashboard));
        }
    }

    // ViewModel for dashboard data
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int NewReviews { get; set; }
        public int ViolationReports { get; set; }
        public int ActiveCompanies { get; set; }
        public List<UserGrowthPoint> UserGrowthData { get; set; } = new List<UserGrowthPoint>();
    }

    public class UserGrowthPoint
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int Count { get; set; }
    }
}