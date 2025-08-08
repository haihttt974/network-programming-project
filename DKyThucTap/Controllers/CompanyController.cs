using DKyThucTap.Models.DTOs.Company;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Route("Company")]
    public class CompanyController : Controller
    {
        private readonly ICompanyService _companyService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            ICompanyService companyService,
            IAuthorizationService authorizationService,
            ILogger<CompanyController> logger)
        {
            _companyService = companyService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        // GET: Company
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null)
        {
            try
            {
                var userId = User.Identity.IsAuthenticated 
                    ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0") 
                    : (int?)null;

                List<CompanyListDto> companies;
                
                if (!string.IsNullOrEmpty(search))
                {
                    companies = await _companyService.SearchCompaniesAsync(search, userId);
                    ViewBag.Search = search;
                }
                else
                {
                    companies = await _companyService.GetAllCompaniesAsync(userId);
                }

                return View(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies index");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách công ty";
                return View(new List<CompanyListDto>());
            }
        }

        // GET: Company/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.Identity.IsAuthenticated 
                    ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0") 
                    : (int?)null;

                var company = await _companyService.GetCompanyByIdAsync(id, userId);
                if (company == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy công ty";
                    return RedirectToAction(nameof(Index));
                }

                return View(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company details: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin công ty";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Company/My
        [HttpGet("My")]
        public async Task<IActionResult> My()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var companies = await _companyService.GetUserCompaniesAsync(userId);
                
                return View(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user companies");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách công ty của bạn";
                return View(new List<CompanyListDto>());
            }
        }

        // GET: Company/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Check permission
            if (!await _authorizationService.HasPermissionAsync(userId, "create_company"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo công ty";
                return RedirectToAction("Dashboard", "Account");
            }

            return View(new CreateCompanyDto());
        }

        // POST: Company/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCompanyDto createDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Check permission
            if (!await _authorizationService.HasPermissionAsync(userId, "create_company"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo công ty";
                return RedirectToAction("Dashboard", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(createDto);
            }

            try
            {
                var result = await _companyService.CreateCompanyAsync(createDto, userId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id = result.Company?.CompanyId });
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return View(createDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo công ty";
                return View(createDto);
            }
        }

        // GET: Company/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                // Check if user can manage this company
                if (!await _companyService.CanUserManageCompanyAsync(id, userId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa công ty này";
                    return RedirectToAction(nameof(My));
                }

                var company = await _companyService.GetCompanyByIdAsync(id, userId);
                if (company == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy công ty";
                    return RedirectToAction(nameof(My));
                }

                var updateDto = new UpdateCompanyDto
                {
                    Name = company.Name,
                    Description = company.Description,
                    LogoUrl = company.LogoUrl,
                    Website = company.Website,
                    Industry = company.Industry,
                    Location = company.Location
                };

                ViewBag.CompanyId = id;
                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit company page: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa";
                return RedirectToAction(nameof(My));
            }
        }

        // POST: Company/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateCompanyDto updateDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!ModelState.IsValid)
            {
                ViewBag.CompanyId = id;
                return View(updateDto);
            }

            try
            {
                var result = await _companyService.UpdateCompanyAsync(id, updateDto, userId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    ViewBag.CompanyId = id;
                    return View(updateDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật công ty";
                ViewBag.CompanyId = id;
                return View(updateDto);
            }
        }

        // POST: Company/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var result = await _companyService.DeleteCompanyAsync(id, userId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction(nameof(My));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa công ty";
                return RedirectToAction(nameof(My));
            }
        }

        // GET: Company/Recruiters/5
        [HttpGet("Recruiters/{id}")]
        public async Task<IActionResult> Recruiters(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                // Check if user can manage this company
                if (!await _companyService.CanUserManageCompanyAsync(id, userId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền quản lý nhân viên của công ty này";
                    return RedirectToAction(nameof(My));
                }

                var company = await _companyService.GetCompanyByIdAsync(id, userId);
                if (company == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy công ty";
                    return RedirectToAction(nameof(My));
                }

                var recruiters = await _companyService.GetCompanyRecruitersAsync(id, userId);
                var pendingRequests = await _companyService.GetPendingRecruiterRequestsAsync(id, userId);
                var statistics = await _companyService.GetCompanyRecruiterStatisticsAsync(id, userId);

                ViewBag.Company = company;
                ViewBag.PendingRequests = pendingRequests;
                ViewBag.Statistics = statistics;

                return View(recruiters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company recruiters: {CompanyId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách nhân viên";
                return RedirectToAction(nameof(My));
            }
        }

        // POST: Company/InviteRecruiter
        [HttpPost("InviteRecruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteRecruiter(CompanyRecruiterInviteDto inviteDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var result = await _companyService.InviteRecruiterAsync(inviteDto.CompanyId, inviteDto.UserEmail, userId, inviteDto.Message);

                return Json(new {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting recruiter");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi lời mời" });
            }
        }

        // POST: Company/RespondToRequest
        [HttpPost("RespondToRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToRequest(CompanyRecruiterResponseDto responseDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var result = await _companyService.RespondToRecruiterRequestAsync(
                    responseDto.CompanyId,
                    responseDto.UserId,
                    responseDto.IsApproved,
                    userId,
                    responseDto.ResponseMessage);

                return Json(new {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to recruiter request");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý yêu cầu" });
            }
        }

        // POST: Company/RemoveRecruiter
        [HttpPost("RemoveRecruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRecruiter(int companyId, int recruiterId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var result = await _companyService.RemoveRecruiterAsync(companyId, recruiterId, userId);

                return Json(new {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing recruiter");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa nhân viên" });
            }
        }

        // POST: Company/RequestToJoin
        [HttpPost("RequestToJoin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestToJoin(CompanyRecruiterRequestDto requestDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var result = await _companyService.RequestToJoinCompanyAsync(requestDto.CompanyId, userId, requestDto.Message);

                return Json(new {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting to join company");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi yêu cầu tham gia" });
            }
        }

        // POST: Company/Leave
        [HttpPost("Leave")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int companyId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var result = await _companyService.LeaveCompanyAsync(companyId, userId);

                return Json(new {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving company");
                return Json(new { success = false, message = "Có lỗi xảy ra khi rời khỏi công ty" });
            }
        }

        // POST: Company/RespondToInvitation
        [HttpPost("RespondToInvitation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToInvitation(int companyId, bool accept, string? responseMessage = null)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                // Find the invitation
                var invitation = await _companyService.GetUserCompanyRequestsAsync(userId);
                var currentInvitation = invitation.FirstOrDefault(i => i.CompanyId == companyId && i.Status == "invited");

                if (currentInvitation == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lời mời" });
                }

                if (accept)
                {
                    // Accept the invitation by approving it
                    var result = await _companyService.RespondToRecruiterRequestAsync(companyId, userId, true, userId, responseMessage ?? "Đã chấp nhận lời mời");
                    return Json(new {
                        success = result.Success,
                        message = result.Success ? "Đã chấp nhận lời mời thành công" : result.Message
                    });
                }
                else
                {
                    // Decline the invitation by rejecting it
                    var result = await _companyService.RespondToRecruiterRequestAsync(companyId, userId, false, userId, responseMessage ?? "Đã từ chối lời mời");
                    return Json(new {
                        success = result.Success,
                        message = result.Success ? "Đã từ chối lời mời" : result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to invitation for company {CompanyId}", companyId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi phản hồi lời mời" });
            }
        }

        // GET: Company/MyRequests
        [HttpGet("MyRequests")]
        public async Task<IActionResult> MyRequests()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var requests = await _companyService.GetUserCompanyRequestsAsync(userId);

                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user company requests");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách yêu cầu";
                return View(new List<CompanyRecruiterListDto>());
            }
        }

        // GET: Company/Debug (for testing purposes)
        [HttpGet("Debug")]
        public IActionResult Debug()
        {
            return View();
        }
    }
}
