using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Models.DTOs.Application;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<ApplicationService> _logger;
        private readonly INotificationIntegrationService _notificationIntegration;

        public ApplicationService(
            DKyThucTapContext context,
            ILogger<ApplicationService> logger,
            INotificationIntegrationService notificationIntegration)
        {
            _context = context;
            _logger = logger;
            _notificationIntegration = notificationIntegration;
        }

        public async Task<ApplicationDetailDto?> GetApplicationByIdAsync(int applicationId, int userId)
        {
            try
            {
                if (!await CanUserViewApplicationAsync(applicationId, userId))
                {
                    return null;
                }

                var application = await _context.Applications
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Include(a => a.ApplicationStatusHistories)
                        .ThenInclude(h => h.ChangedByNavigation)
                            .ThenInclude(u => u.UserProfile)
                    .Include(a => a.ApplicantNotes)
                        .ThenInclude(n => n.InterviewerUser)
                            .ThenInclude(u => u.UserProfile)
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserSkills)
                            .ThenInclude(us => us.Skill)
                    .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

                if (application == null) return null;

                return new ApplicationDetailDto
                {
                    ApplicationId = application.ApplicationId,
                    PositionId = application.PositionId,
                    PositionTitle = application.Position.Title,
                    CompanyName = application.Position.Company.Name,
                    UserId = application.UserId,
                    CurrentStatus = application.CurrentStatus,
                    AppliedAt = application.AppliedAt,
                    CoverLetter = application.CoverLetter,
                    AdditionalInfo = application.AdditionalInfo,

                    ApplicantName = application.User.UserProfile != null
                        ? $"{application.User.UserProfile.FirstName} {application.User.UserProfile.LastName}".Trim()
                        : application.User.Email,
                    ApplicantEmail = application.User.Email,
                    ApplicantPhone = application.User.UserProfile?.Phone,
                    ApplicantAddress = application.User.UserProfile?.Address,
                    ApplicantBio = application.User.UserProfile?.Bio,
                    ApplicantProfilePictureUrl = application.User.UserProfile?.ProfilePictureUrl,
                    ApplicantCvUrl = application.User.UserProfile?.CvUrl,

                    StatusHistory = application.ApplicationStatusHistories
                        .OrderByDescending(h => h.ChangedAt)
                        .Select(h => new ApplicationStatusHistoryDto
                        {
                            HistoryId = h.HistoryId,
                            ApplicationId = h.ApplicationId,
                            Status = h.Status,
                            ChangedAt = h.ChangedAt,
                            ChangedBy = h.ChangedBy,
                            ChangedByName = h.ChangedByNavigation?.UserProfile != null
                                ? $"{h.ChangedByNavigation.UserProfile.FirstName} {h.ChangedByNavigation.UserProfile.LastName}".Trim()
                                : h.ChangedByNavigation?.Email ?? "System",
                            Notes = h.Notes
                        }).ToList(),

                    Notes = application.ApplicantNotes
                        .OrderByDescending(n => n.CreatedAt)
                        .Select(n => new ApplicantNoteDto
                        {
                            NoteId = n.NoteId,
                            ApplicationId = n.ApplicationId,
                            InterviewerUserId = n.InterviewerUserId,
                            InterviewerName = n.InterviewerUser.UserProfile != null
                                ? $"{n.InterviewerUser.UserProfile.FirstName} {n.InterviewerUser.UserProfile.LastName}".Trim()
                                : n.InterviewerUser.Email,
                            NoteText = n.NoteText,
                            CreatedAt = n.CreatedAt
                        }).ToList(),

                    ApplicantSkills = application.User.UserSkills
                        .Select(us => new UserSkillDto
                        {
                            SkillId = us.SkillId,
                            SkillName = us.Skill.Name,
                            SkillCategory = us.Skill.Category,
                            ProficiencyLevel = us.ProficiencyLevel
                        }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application by ID: {ApplicationId}", applicationId);
                return null;
            }
        }

        public async Task<ApplicationSearchResultDto> GetApplicationsAsync(ApplicationSearchDto searchDto, int userId)
        {
            try
            {
                var query = _context.Applications
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Include(a => a.ApplicantNotes)
                    .AsQueryable();

                // Apply user permissions filter
                query = await ApplyUserPermissionsFilterAsync(query, userId);

                // Apply search filters
                if (searchDto.PositionId.HasValue)
                {
                    query = query.Where(a => a.PositionId == searchDto.PositionId.Value);
                }

                if (searchDto.CompanyId.HasValue)
                {
                    query = query.Where(a => a.Position.CompanyId == searchDto.CompanyId.Value);
                }

                if (!string.IsNullOrEmpty(searchDto.Status))
                {
                    query = query.Where(a => a.CurrentStatus == searchDto.Status);
                }

                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    query = query.Where(a =>
                        a.User.Email.Contains(searchDto.SearchTerm) ||
                        (a.User.UserProfile != null && 
                         (a.User.UserProfile.FirstName.Contains(searchDto.SearchTerm) ||
                          a.User.UserProfile.LastName.Contains(searchDto.SearchTerm))) ||
                        a.Position.Title.Contains(searchDto.SearchTerm) ||
                        a.Position.Company.Name.Contains(searchDto.SearchTerm));
                }

                if (searchDto.AppliedFrom.HasValue)
                {
                    query = query.Where(a => a.AppliedAt >= searchDto.AppliedFrom.Value);
                }

                if (searchDto.AppliedTo.HasValue)
                {
                    query = query.Where(a => a.AppliedAt <= searchDto.AppliedTo.Value);
                }

                // Apply sorting
                query = searchDto.SortBy.ToLower() switch
                {
                    "applicantname" => searchDto.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(a => a.User.UserProfile != null ? a.User.UserProfile.FirstName : a.User.Email)
                        : query.OrderBy(a => a.User.UserProfile != null ? a.User.UserProfile.FirstName : a.User.Email),
                    "positiontitle" => searchDto.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(a => a.Position.Title)
                        : query.OrderBy(a => a.Position.Title),
                    "status" => searchDto.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(a => a.CurrentStatus)
                        : query.OrderBy(a => a.CurrentStatus),
                    _ => searchDto.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(a => a.AppliedAt)
                        : query.OrderBy(a => a.AppliedAt)
                };

                var totalCount = await query.CountAsync();

                var applications = await query
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .Select(a => new ApplicationListDto
                    {
                        ApplicationId = a.ApplicationId,
                        PositionId = a.PositionId,
                        PositionTitle = a.Position.Title,
                        CompanyName = a.Position.Company.Name,
                        UserId = a.UserId,
                        ApplicantName = a.User.UserProfile != null
                            ? $"{a.User.UserProfile.FirstName} {a.User.UserProfile.LastName}".Trim()
                            : a.User.Email,
                        ApplicantEmail = a.User.Email,
                        ApplicantPhone = a.User.UserProfile != null ? a.User.UserProfile.Phone : null,
                        ApplicantProfilePictureUrl = a.User.UserProfile != null ? a.User.UserProfile.ProfilePictureUrl : null,
                        CurrentStatus = a.CurrentStatus,
                        AppliedAt = a.AppliedAt,
                        NotesCount = a.ApplicantNotes.Count,
                        HasCoverLetter = !string.IsNullOrEmpty(a.CoverLetter),
                        HasCv = a.User.UserProfile != null && !string.IsNullOrEmpty(a.User.UserProfile.CvUrl)
                    })
                    .ToListAsync();

                return new ApplicationSearchResultDto
                {
                    Applications = applications,
                    TotalCount = totalCount,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching applications");
                return new ApplicationSearchResultDto();
            }
        }

        private async Task<IQueryable<Application>> ApplyUserPermissionsFilterAsync(IQueryable<Application> query, int userId)
        {
            // Check if user is admin
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.Role?.RoleName == "Admin")
            {
                return query; // Admin can see all applications
            }

            // For recruiters, only show applications for positions they can manage
            var userCompanyIds = await _context.CompanyRecruiters
                .Where(cr => cr.UserId == userId && cr.IsApproved == true)
                .Select(cr => cr.CompanyId)
                .ToListAsync();

            var ownedCompanyIds = await _context.Companies
                .Where(c => c.CreatedBy == userId)
                .Select(c => c.CompanyId)
                .ToListAsync();

            var allCompanyIds = userCompanyIds.Concat(ownedCompanyIds).Distinct().ToList();

            if (allCompanyIds.Any())
            {
                query = query.Where(a => allCompanyIds.Contains(a.Position.CompanyId));
            }
            else
            {
                // If user has no companies, only show their own applications
                query = query.Where(a => a.UserId == userId);
            }

            return query;
        }

        // Additional methods will be implemented in the next part...
        public async Task<List<ApplicationListDto>> GetApplicationsByPositionAsync(int positionId, int userId)
        {
            if (!await CanUserManagePositionApplicationsAsync(positionId, userId))
            {
                return new List<ApplicationListDto>();
            }

            return await _context.Applications
                .Include(a => a.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(a => a.ApplicantNotes)
                .Where(a => a.PositionId == positionId)
                .OrderByDescending(a => a.AppliedAt)
                .Select(a => new ApplicationListDto
                {
                    ApplicationId = a.ApplicationId,
                    PositionId = a.PositionId,
                    UserId = a.UserId,
                    ApplicantName = a.User.UserProfile != null
                        ? $"{a.User.UserProfile.FirstName} {a.User.UserProfile.LastName}".Trim()
                        : a.User.Email,
                    ApplicantEmail = a.User.Email,
                    ApplicantPhone = a.User.UserProfile != null ? a.User.UserProfile.Phone : null,
                    ApplicantProfilePictureUrl = a.User.UserProfile != null ? a.User.UserProfile.ProfilePictureUrl : null,
                    CurrentStatus = a.CurrentStatus,
                    AppliedAt = a.AppliedAt,
                    NotesCount = a.ApplicantNotes.Count,
                    HasCoverLetter = !string.IsNullOrEmpty(a.CoverLetter),
                    HasCv = a.User.UserProfile != null && !string.IsNullOrEmpty(a.User.UserProfile.CvUrl)
                })
                .ToListAsync();
        }

        public async Task<bool> CanUserManageApplicationAsync(int applicationId, int userId)
        {
            try
            {
                var application = await _context.Applications
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                            .ThenInclude(c => c.CompanyRecruiters)
                    .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

                if (application == null) return false;

                // Check if user is admin
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.Role?.RoleName == "Admin") return true;

                // Check if user is company owner
                if (application.Position.Company.CreatedBy == userId) return true;

                // Check if user is approved recruiter for the company
                return application.Position.Company.CompanyRecruiters
                    .Any(cr => cr.UserId == userId && cr.IsApproved == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking application management permission: {ApplicationId}, {UserId}", applicationId, userId);
                return false;
            }
        }

        public async Task<bool> CanUserViewApplicationAsync(int applicationId, int userId)
        {
            // For now, same as manage permission, but could be different in the future
            return await CanUserManageApplicationAsync(applicationId, userId);
        }

        public async Task<bool> CanUserManagePositionApplicationsAsync(int positionId, int userId)
        {
            try
            {
                var position = await _context.Positions
                    .Include(p => p.Company)
                        .ThenInclude(c => c.CompanyRecruiters)
                    .FirstOrDefaultAsync(p => p.PositionId == positionId);

                if (position == null) return false;

                // Check if user is admin
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.Role?.RoleName == "Admin") return true;

                // Check if user is company owner
                if (position.Company.CreatedBy == userId) return true;

                // Check if user is approved recruiter for the company
                return position.Company.CompanyRecruiters
                    .Any(cr => cr.UserId == userId && cr.IsApproved == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking position applications management permission: {PositionId}, {UserId}", positionId, userId);
                return false;
            }
        }

        public async Task<(bool Success, string Message)> UpdateApplicationStatusAsync(int applicationId, string newStatus, string? notes, int changedBy)
        {
            try
            {
                if (!await CanUserManageApplicationAsync(applicationId, changedBy))
                {
                    return (false, "Bạn không có quyền thay đổi trạng thái đơn ứng tuyển này");
                }

                var application = await _context.Applications
                    .Include(a => a.Position)
                        .ThenInclude(p => p.Company)
                    .Include(a => a.User)
                        .ThenInclude(u => u.UserProfile)
                    .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

                if (application == null)
                {
                    return (false, "Không tìm thấy đơn ứng tuyển");
                }

                var oldStatus = application.CurrentStatus;
                if (!await IsValidStatusTransitionAsync(oldStatus, newStatus))
                {
                    return (false, $"Không thể chuyển từ trạng thái '{oldStatus}' sang '{newStatus}'");
                }

                // Update application status
                application.CurrentStatus = newStatus;

                // Create status history record
                var statusHistory = new ApplicationStatusHistory
                {
                    ApplicationId = applicationId,
                    Status = newStatus,
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = changedBy,
                    Notes = notes
                };

                _context.ApplicationStatusHistories.Add(statusHistory);
                await _context.SaveChangesAsync();

                // Send notification to applicant
                await SendApplicationStatusChangeNotificationAsync(application, oldStatus, newStatus);

                _logger.LogInformation("Application status updated: {ApplicationId} from {OldStatus} to {NewStatus} by {ChangedBy}",
                    applicationId, oldStatus, newStatus, changedBy);

                return (true, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status: {ApplicationId}", applicationId);
                return (false, "Có lỗi xảy ra khi cập nhật trạng thái");
            }
        }

        public async Task<(bool Success, string Message)> AddApplicantNoteAsync(int applicationId, string noteText, int interviewerUserId)
        {
            try
            {
                if (!await CanUserManageApplicationAsync(applicationId, interviewerUserId))
                {
                    return (false, "Bạn không có quyền thêm ghi chú cho đơn ứng tuyển này");
                }

                var note = new ApplicantNote
                {
                    ApplicationId = applicationId,
                    InterviewerUserId = interviewerUserId,
                    NoteText = noteText,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.ApplicantNotes.Add(note);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Applicant note added: {ApplicationId} by {InterviewerUserId}", applicationId, interviewerUserId);
                return (true, "Thêm ghi chú thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding applicant note: {ApplicationId}", applicationId);
                return (false, "Có lỗi xảy ra khi thêm ghi chú");
            }
        }

        public async Task<ApplicationStatisticsDto> GetApplicationStatisticsAsync(int? positionId, int? companyId, int userId)
        {
            try
            {
                var query = _context.Applications.AsQueryable();

                // Apply user permissions
                query = await ApplyUserPermissionsFilterAsync(query, userId);

                if (positionId.HasValue)
                {
                    query = query.Where(a => a.PositionId == positionId.Value);
                }

                if (companyId.HasValue)
                {
                    query = query.Where(a => a.Position.CompanyId == companyId.Value);
                }

                var applications = await query.ToListAsync();
                var totalCount = applications.Count;

                if (totalCount == 0)
                {
                    return new ApplicationStatisticsDto();
                }

                var statusCounts = applications
                    .GroupBy(a => a.CurrentStatus)
                    .Select(g => new ApplicationStatusCountDto
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Percentage = (double)g.Count() / totalCount * 100
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();

                var acceptedCount = applications.Count(a => a.CurrentStatus == "accepted");
                var acceptanceRate = totalCount > 0 ? (double)acceptedCount / totalCount * 100 : 0;

                return new ApplicationStatisticsDto
                {
                    TotalApplications = totalCount,
                    PendingApplications = applications.Count(a => a.CurrentStatus == "applied"),
                    ReviewingApplications = applications.Count(a => a.CurrentStatus == "reviewing"),
                    AcceptedApplications = acceptedCount,
                    RejectedApplications = applications.Count(a => a.CurrentStatus == "rejected"),
                    AcceptanceRate = acceptanceRate,
                    StatusCounts = statusCounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application statistics");
                return new ApplicationStatisticsDto();
            }
        }

        private async Task SendApplicationStatusChangeNotificationAsync(Application application, string oldStatus, string newStatus)
        {
            try
            {
                var statusMessage = newStatus switch
                {
                    "reviewing" => "đang được xem xét",
                    "interviewed" => "đã được mời phỏng vấn",
                    "accepted" => "đã được chấp nhận",
                    "rejected" => "đã bị từ chối",
                    _ => $"đã chuyển sang trạng thái {newStatus}"
                };

                await _notificationIntegration.NotifyJobApplicationStatusChangedAsync(
                    application.UserId,
                    application.Position.Title,
                    statusMessage,
                    application.ApplicationId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application status change notification");
            }
        }

        public async Task<bool> IsValidStatusTransitionAsync(string currentStatus, string newStatus)
        {
            // Define valid status transitions
            var validTransitions = new Dictionary<string, List<string>>
            {
                ["applied"] = new List<string> { "reviewing", "rejected" },
                ["reviewing"] = new List<string> { "interviewed", "accepted", "rejected" },
                ["interviewed"] = new List<string> { "accepted", "rejected" },
                ["accepted"] = new List<string>(), // Final state
                ["rejected"] = new List<string>()  // Final state
            };

            return validTransitions.ContainsKey(currentStatus) &&
                   validTransitions[currentStatus].Contains(newStatus);
        }

        public async Task<List<string>> GetAvailableStatusesAsync(string currentStatus)
        {
            var validTransitions = new Dictionary<string, List<string>>
            {
                ["applied"] = new List<string> { "reviewing", "rejected" },
                ["reviewing"] = new List<string> { "interviewed", "accepted", "rejected" },
                ["interviewed"] = new List<string> { "accepted", "rejected" },
                ["accepted"] = new List<string>(),
                ["rejected"] = new List<string>()
            };

            return validTransitions.ContainsKey(currentStatus)
                ? validTransitions[currentStatus]
                : new List<string>();
        }

        public async Task<bool> ApplicationExistsAsync(int applicationId)
        {
            return await _context.Applications.AnyAsync(a => a.ApplicationId == applicationId);
        }

        // Remaining placeholder methods
        public async Task<List<ApplicationListDto>> GetApplicationsByUserAsync(int userId) => new List<ApplicationListDto>();
        public async Task<(bool Success, string Message, int UpdatedCount)> BulkUpdateApplicationStatusAsync(List<int> applicationIds, string newStatus, string? notes, int changedBy) => (false, "Not implemented", 0);
        public async Task<List<ApplicationStatusHistoryDto>> GetApplicationStatusHistoryAsync(int applicationId, int userId) => new List<ApplicationStatusHistoryDto>();
        public async Task<List<ApplicantNoteDto>> GetApplicantNotesAsync(int applicationId, int userId) => new List<ApplicantNoteDto>();
        public async Task<(bool Success, string Message)> DeleteApplicantNoteAsync(int noteId, int userId) => (false, "Not implemented");
        public async Task<List<ApplicationListDto>> GetRecentApplicationsAsync(int userId, int count = 10) => new List<ApplicationListDto>();
        public async Task<(bool Success, string Message)> BulkDeleteApplicationsAsync(List<int> applicationIds, int userId) => (false, "Not implemented");
        public async Task<List<ApplicationListDto>> GetApplicationsForBulkActionAsync(List<int> applicationIds, int userId) => new List<ApplicationListDto>();
    }
}
