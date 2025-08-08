using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DKyThucTap.Services;
using DKyThucTap.Models.DTOs.Application;
using DKyThucTap.Models.ViewModels;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Authorize]
    [Route("ApplicationManagement")]
    public class ApplicationManagementController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IPositionService _positionService;
        private readonly ILogger<ApplicationManagementController> _logger;

        public ApplicationManagementController(
            IApplicationService applicationService,
            IPositionService positionService,
            ILogger<ApplicationManagementController> logger)
        {
            _applicationService = applicationService;
            _positionService = positionService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        // GET: ApplicationManagement/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var application = await _applicationService.GetApplicationByIdAsync(id, userId);

                if (application == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển hoặc bạn không có quyền xem";
                    return RedirectToAction("Index", "Home");
                }

                var position = await _positionService.GetPositionByIdAsync(application.PositionId);
                var canManage = await _applicationService.CanUserManageApplicationAsync(id, userId);

                var viewModel = new ApplicationDetailViewModel
                {
                    Application = application,
                    Position = position ?? new Models.DTOs.Position.PositionDetailDto(),
                    CanManageApplication = canManage,
                    CanAddNotes = canManage
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading application details: {ApplicationId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn ứng tuyển";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: ApplicationManagement/ByPosition/5
        [HttpGet("ByPosition/{positionId}")]
        public async Task<IActionResult> ByPosition(int positionId, int page = 1, int pageSize = 20, string? status = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                if (!await _applicationService.CanUserManagePositionApplicationsAsync(positionId, userId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem đơn ứng tuyển cho vị trí này";
                    return RedirectToAction("Index", "Position");
                }

                var position = await _positionService.GetPositionByIdAsync(positionId);
                if (position == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy vị trí tuyển dụng";
                    return RedirectToAction("Index", "Position");
                }

                var searchDto = new ApplicationSearchDto
                {
                    PositionId = positionId,
                    Status = status,
                    Page = page,
                    PageSize = pageSize
                };

                var searchResult = await _applicationService.GetApplicationsAsync(searchDto, userId);
                var statistics = await _applicationService.GetApplicationStatisticsAsync(positionId, null, userId);

                var viewModel = new ApplicationManagementViewModel
                {
                    Position = position,
                    Applications = searchResult.Applications,
                    Statistics = statistics,
                    SearchCriteria = searchDto,
                    TotalCount = searchResult.TotalCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CanManageApplications = true,
                    CanViewApplicantDetails = true,
                    CanAddNotes = true
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading applications for position: {PositionId}", positionId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn ứng tuyển";
                return RedirectToAction("Details", "Position", new { id = positionId });
            }
        }

        // POST: ApplicationManagement/UpdateStatus
        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromForm] UpdateApplicationStatusDto model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _applicationService.UpdateApplicationStatusAsync(
                    model.ApplicationId, 
                    model.NewStatus, 
                    model.Notes, 
                    userId);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("Details", new { id = model.ApplicationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status: {ApplicationId}", model.ApplicationId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái";
                return RedirectToAction("Details", new { id = model.ApplicationId });
            }
        }

        // POST: ApplicationManagement/AddNote
        [HttpPost("AddNote")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote([FromForm] AddApplicantNoteDto model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _applicationService.AddApplicantNoteAsync(
                    model.ApplicationId, 
                    model.NoteText, 
                    userId);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("Details", new { id = model.ApplicationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding applicant note: {ApplicationId}", model.ApplicationId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm ghi chú";
                return RedirectToAction("Details", new { id = model.ApplicationId });
            }
        }

        // GET: ApplicationManagement/Index
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? status = null, string? search = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var searchDto = new ApplicationSearchDto
                {
                    Status = status,
                    SearchTerm = search,
                    Page = page,
                    PageSize = pageSize
                };

                var searchResult = await _applicationService.GetApplicationsAsync(searchDto, userId);
                var statistics = await _applicationService.GetApplicationStatisticsAsync(null, null, userId);

                var viewModel = new ApplicationManagementViewModel
                {
                    Applications = searchResult.Applications,
                    Statistics = statistics,
                    SearchCriteria = searchDto,
                    TotalCount = searchResult.TotalCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CanManageApplications = true,
                    CanViewApplicantDetails = true,
                    CanAddNotes = true
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading applications index");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn ứng tuyển";
                return View(new ApplicationManagementViewModel());
            }
        }

        // API endpoint for getting application statistics
        [HttpGet("api/statistics")]
        public async Task<IActionResult> GetStatistics(int? positionId = null, int? companyId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var statistics = await _applicationService.GetApplicationStatisticsAsync(positionId, companyId, userId);
                return Json(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application statistics");
                return Json(new ApplicationStatisticsDto());
            }
        }

        // API endpoint for getting available statuses
        [HttpGet("api/available-statuses")]
        public async Task<IActionResult> GetAvailableStatuses(string currentStatus)
        {
            try
            {
                var statuses = await _applicationService.GetAvailableStatusesAsync(currentStatus);
                return Json(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available statuses");
                return Json(new List<string>());
            }
        }

        // POST: ApplicationManagement/BulkUpdateStatus
        [HttpPost("BulkUpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus([FromForm] BulkApplicationActionViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                if (!model.SelectedApplicationIds.Any())
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một đơn ứng tuyển";
                    return RedirectToAction("ByPosition", new { positionId = model.PositionId });
                }

                var result = await _applicationService.BulkUpdateApplicationStatusAsync(
                    model.SelectedApplicationIds,
                    model.NewStatus,
                    model.Notes,
                    userId);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Đã cập nhật trạng thái cho {result.UpdatedCount} đơn ứng tuyển";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("ByPosition", new { positionId = model.PositionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating application status");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật hàng loạt";
                return RedirectToAction("ByPosition", new { positionId = model.PositionId });
            }
        }
    }
}
