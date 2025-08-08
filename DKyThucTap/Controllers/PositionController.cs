using DKyThucTap.Models.DTOs.Position;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Route("Position")]
    public class PositionController : Controller
    {
        private readonly IPositionService _positionService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<PositionController> _logger;

        public PositionController(
            IPositionService positionService,
            IAuthorizationService authorizationService,
            ILogger<PositionController> logger)
        {
            _positionService = positionService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        // GET: Position
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12, string? search = null)
        {
            try
            {
                var searchDto = new PositionSearchDto
                {
                    SearchTerm = search,
                    Page = page,
                    PageSize = pageSize,
                    IsActive = true
                };

                var result = await _positionService.GetPositionsAsync(searchDto);
                
                ViewBag.Search = search;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading positions index");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách vị trí";
                return View(new PositionSearchResultDto());
            }
        }

        // GET: Position/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var position = await _positionService.GetPositionByIdAsync(id);
                if (position == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy vị trí";
                    return RedirectToAction(nameof(Index));
                }

                return View(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading position details: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin vị trí";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Position/My
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
                var positions = await _positionService.GetPositionsByUserAsync(userId);
                
                return View(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user positions");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách vị trí của bạn";
                return View(new List<PositionListDto>());
            }
        }

        // GET: Position/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Check permission
            if (!await _authorizationService.HasPermissionAsync(userId, "create_position"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo vị trí tuyển dụng";
                return RedirectToAction("Dashboard", "Account");
            }

            try
            {
                await LoadCreateEditViewData(userId);
                return View(new CreatePositionDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create position page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo vị trí";
                return RedirectToAction("Dashboard", "Account");
            }
        }

        // POST: Position/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePositionDto createDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Check permission
            if (!await _authorizationService.HasPermissionAsync(userId, "create_position"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo vị trí tuyển dụng";
                return RedirectToAction("Dashboard", "Account");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateEditViewData(userId);
                return View(createDto);
            }

            try
            {
                var result = await _positionService.CreatePositionAsync(createDto, userId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id = result.Position?.PositionId });
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    await LoadCreateEditViewData(userId);
                    return View(createDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo vị trí";
                await LoadCreateEditViewData(userId);
                return View(createDto);
            }
        }

        // GET: Position/Edit/5
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
                // Check if user can manage this position
                if (!await _positionService.CanUserManagePositionAsync(id, userId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa vị trí này";
                    return RedirectToAction(nameof(My));
                }

                var position = await _positionService.GetPositionByIdAsync(id);
                if (position == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy vị trí";
                    return RedirectToAction(nameof(My));
                }

                var updateDto = new UpdatePositionDto
                {
                    Title = position.Title,
                    Description = position.Description,
                    PositionType = position.PositionType,
                    Location = position.Location,
                    IsRemote = position.IsRemote ?? false,
                    SalaryRange = position.SalaryRange,
                    ApplicationDeadline = position.ApplicationDeadline,
                    CategoryId = position.CategoryId,
                    IsActive = position.IsActive ?? true,
                    SkillIds = position.RequiredSkills.Select(s => s.SkillId).ToList()
                };

                await LoadCreateEditViewData(userId);
                ViewBag.PositionId = id;
                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit position page: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa";
                return RedirectToAction(nameof(My));
            }
        }

        // POST: Position/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdatePositionDto updateDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!ModelState.IsValid)
            {
                await LoadCreateEditViewData(userId);
                ViewBag.PositionId = id;
                return View(updateDto);
            }

            try
            {
                var result = await _positionService.UpdatePositionAsync(id, updateDto, userId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    await LoadCreateEditViewData(userId);
                    ViewBag.PositionId = id;
                    return View(updateDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật vị trí";
                await LoadCreateEditViewData(userId);
                ViewBag.PositionId = id;
                return View(updateDto);
            }
        }

        // POST: Position/EditAjax/5 - AJAX endpoint for real-time updates
        [HttpPost("EditAjax/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, UpdatePositionDto updateDto)
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
                var result = await _positionService.UpdatePositionAsync(id, updateDto, userId);

                if (result.Success)
                {
                    return Json(new {
                        success = true,
                        message = result.Message,
                        position = result.Position
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position via AJAX: {PositionId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật vị trí" });
            }
        }

        // POST: Position/Delete/5
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
                var result = await _positionService.DeletePositionAsync(id, userId);
                
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
                _logger.LogError(ex, "Error deleting position: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa vị trí";
                return RedirectToAction(nameof(My));
            }
        }

        // POST: Position/UpdateStatus/5
        [HttpPost("UpdateStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, bool isActive)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var result = await _positionService.UpdatePositionStatusAsync(id, isActive, userId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position status: {PositionId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái" });
            }
        }

        // GET: Position/History/5
        [HttpGet("History/{id}")]
        public async Task<IActionResult> History(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                // Check if user can manage this position
                if (!await _positionService.CanUserManagePositionAsync(id, userId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem lịch sử của vị trí này";
                    return RedirectToAction(nameof(My));
                }

                var position = await _positionService.GetPositionByIdAsync(id);
                if (position == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy vị trí";
                    return RedirectToAction(nameof(My));
                }

                var history = await _positionService.GetPositionHistoryAsync(id, userId);

                ViewBag.Position = position;
                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading position history: {PositionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch sử vị trí";
                return RedirectToAction(nameof(My));
            }
        }

        // Helper method to load data for Create/Edit views
        private async Task LoadCreateEditViewData(int userId)
        {
            ViewBag.Companies = await _positionService.GetUserCompaniesAsync(userId);
            ViewBag.Categories = await _positionService.GetJobCategoriesAsync();
            ViewBag.Skills = await _positionService.GetSkillsAsync();
        }
    }
}
