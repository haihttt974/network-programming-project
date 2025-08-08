using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Authorize(Roles = "Candidate")] // chỉ ứng viên mới được ứng tuyển
    public class ApplicationsController : Controller
    {
        private readonly DKyThucTapContext _context;
        private readonly INotificationIntegrationService _notificationIntegration;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(
            DKyThucTapContext context,
            INotificationIntegrationService notificationIntegration,
            ILogger<ApplicationsController> logger)
        {
            _context = context;
            _notificationIntegration = notificationIntegration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Apply(int positionId)
        {
            var position = await _context.Positions
                .Include(p => p.Company)
                .Include(p => p.PositionSkills).ThenInclude(ps => ps.Skill)
                .FirstOrDefaultAsync(p => p.PositionId == positionId);

            if (position == null)
            {
                TempData["Error"] = "Không tìm thấy công việc này.";
                return RedirectToAction("Index", "Positions");
            }

            return View(position);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int positionId, string coverLetter, IFormFile? cvFile)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["Error"] = "Bạn cần đăng nhập để ứng tuyển.";
                return RedirectToAction("Login", "Auth");
            }

            int userId = int.Parse(userIdClaim.Value);

            if (await _context.Applications.AnyAsync(a => a.PositionId == positionId && a.UserId == userId))
            {
                TempData["Warning"] = "Bạn đã ứng tuyển công việc này.";
                return RedirectToAction("Details", "Position", new { id = positionId });
            }

            string? cvPath = null;
            if (cvFile != null && cvFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/document/application");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                var extension = Path.GetExtension(cvFile.FileName);
                var fileName = $"cv-{userId}{extension}";
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await cvFile.CopyToAsync(stream);
                }

                cvPath = $"/document/application/{fileName}";
            }

            var application = new Application
            {
                PositionId = positionId,
                UserId = userId,
                CoverLetter = coverLetter,
                AdditionalInfo = cvPath,
                CurrentStatus = "applied"
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Send real-time notifications to recruiters
            await SendApplicationNotificationsAsync(application.ApplicationId, positionId, userId);

            TempData["Success"] = "Ứng tuyển thành công!";
            return RedirectToAction("Details", "Position", new { id = positionId });
        }

        private async Task SendApplicationNotificationsAsync(int applicationId, int positionId, int applicantUserId)
        {
            try
            {
                _logger.LogInformation("Sending application notifications for application: {ApplicationId}", applicationId);

                // Get position and company information
                var position = await _context.Positions
                    .Include(p => p.Company)
                        .ThenInclude(c => c.CompanyRecruiters)
                    .FirstOrDefaultAsync(p => p.PositionId == positionId);

                if (position == null) return;

                // Get applicant information
                var applicant = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.UserId == applicantUserId);

                if (applicant == null) return;

                var applicantName = applicant.UserProfile != null
                    ? $"{applicant.UserProfile.FirstName} {applicant.UserProfile.LastName}".Trim()
                    : applicant.Email;

                // Get all recruiters who should be notified
                var recruitersToNotify = new List<int>();

                // Add company owner
                if (position.Company.CreatedBy.HasValue)
                {
                    recruitersToNotify.Add(position.Company.CreatedBy.Value);
                }

                // Add approved company recruiters
                var companyRecruiters = position.Company.CompanyRecruiters
                    .Where(cr => cr.IsApproved == true)
                    .Select(cr => cr.UserId)
                    .ToList();

                recruitersToNotify.AddRange(companyRecruiters);

                // Remove duplicates
                recruitersToNotify = recruitersToNotify.Distinct().ToList();

                // Send notifications to all relevant recruiters
                foreach (var recruiterId in recruitersToNotify)
                {
                    await _notificationIntegration.NotifyJobApplicationSubmittedAsync(
                        recruiterId,
                        position.Title,
                        applicationId
                    );
                }

                _logger.LogInformation("Sent application notifications to {Count} recruiters for application {ApplicationId}",
                    recruitersToNotify.Count, applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application notifications for application: {ApplicationId}", applicationId);
                // Don't throw - notifications shouldn't break the application process
            }
        }
    }
}
