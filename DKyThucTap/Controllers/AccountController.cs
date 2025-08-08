using DKyThucTap.Models.DTOs;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly Services.IAuthorizationService _authorizationService;
        private readonly IPositionService _positionService;
        private readonly ICompanyService _companyService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            Services.IAuthorizationService authorizationService,
            IPositionService positionService,
            ICompanyService companyService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _authorizationService = authorizationService;
            _positionService = positionService;
            _companyService = companyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userProfile = await _authService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return RedirectToAction("Login", "Auth");
            }


            var permissions = await _authorizationService.GetUserPermissionsAsync(userId);
            ViewBag.Permissions = permissions;

            // Load position statistics for recruiters
            if (permissions.ContainsKey("create_position") && permissions["create_position"])
            {
                try
                {
                    var positionStats = await _positionService.GetPositionStatisticsAsync(userId);
                    ViewBag.PositionStatistics = positionStats;

                    // Get user's companies
                    var userCompanies = await _companyService.GetUserCompaniesAsync(userId);
                    ViewBag.UserCompanies = userCompanies.Take(3).ToList();
                    ViewBag.TotalCompanies = userCompanies.Count;

                    // Get pending recruiter requests for companies the user owns
                    var pendingRequests = new List<DKyThucTap.Models.DTOs.Company.CompanyRecruiterListDto>();
                    foreach (var company in userCompanies.Where(c => c.UserRole == "Owner"))
                    {
                        var companyRequests = await _companyService.GetPendingRecruiterRequestsAsync(company.CompanyId, userId);
                        pendingRequests.AddRange(companyRequests);
                    }
                    ViewBag.PendingRecruiterRequests = pendingRequests.Take(5).ToList();

                    // Get user's own pending company requests
                    var userRequests = await _companyService.GetUserCompanyRequestsAsync(userId);
                    ViewBag.UserCompanyRequests = userRequests.Take(3).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading position statistics for user: {UserId}", userId);
                    ViewBag.PositionStatistics = null;
                    ViewBag.UserCompanies = new List<DKyThucTap.Models.DTOs.Company.CompanyListDto>();
                    ViewBag.TotalCompanies = 0;
                }
            }

            return View(userProfile);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userProfile = await _authService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return NotFound();
            }

            var updateDto = new UpdateProfileDto
            {
                FirstName = userProfile.FirstName ?? "",
                LastName = userProfile.LastName ?? "",
                Phone = userProfile.Phone,
                Address = userProfile.Address,
                Bio = userProfile.Bio,
                ProfilePictureUrl = userProfile.ProfilePictureUrl,
                CvUrl = userProfile.CvUrl
            };

            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateProfileDto model, IFormFile? ProfileImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Xử lý lưu ảnh nếu có upload
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, $"{userId}.jpg");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                // Cập nhật URL ảnh cho model (để hiển thị lại)
                model.ProfilePictureUrl = $"/images/profiles/{userId}.jpg";
            }

            var result = await _authService.UpdateUserProfileAsync(userId, model);

            if (result)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Dashboard");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userProfile = await _authService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return NotFound();
            }

            return View(userProfile);
        }
    }
}
