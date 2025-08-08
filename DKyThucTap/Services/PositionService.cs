using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Models.DTOs.Position;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Services
{
    public class PositionService : IPositionService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<PositionService> _logger;
        private readonly INotificationIntegrationService _notificationIntegration;

        public PositionService(
            DKyThucTapContext context,
            ILogger<PositionService> logger,
            INotificationIntegrationService notificationIntegration)
        {
            _context = context;
            _logger = logger;
            _notificationIntegration = notificationIntegration;
        }

        public async Task<(bool Success, string Message, PositionDetailDto? Position)> CreatePositionAsync(CreatePositionDto createDto, int createdBy)
        {
            try
            {
                _logger.LogInformation("Creating position: {Title} by user: {UserId}", createDto.Title, createdBy);

                // Validate company access - user must be owner or approved recruiter
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyId == createDto.CompanyId);

                if (company == null)
                {
                    return (false, "Không tìm thấy công ty", null);
                }

                // Check if user is company owner
                bool isOwner = company.CreatedBy == createdBy;

                // Check if user is approved recruiter
                bool isApprovedRecruiter = false;
                if (!isOwner)
                {
                    isApprovedRecruiter = await _context.CompanyRecruiters
                        .AnyAsync(cr => cr.CompanyId == createDto.CompanyId
                                     && cr.UserId == createdBy
                                     && cr.IsApproved == true
                                     && cr.Status == "approved");
                }

                if (!isOwner && !isApprovedRecruiter)
                {
                    return (false, "Bạn không có quyền tạo vị trí cho công ty này. Chỉ chủ công ty hoặc nhân viên tuyển dụng được phê duyệt mới có thể tạo vị trí.", null);
                }

                // Create position
                var position = new Position
                {
                    CompanyId = createDto.CompanyId,
                    Title = createDto.Title,
                    Description = createDto.Description,
                    PositionType = createDto.PositionType,
                    Location = createDto.Location,
                    IsRemote = createDto.IsRemote,
                    SalaryRange = createDto.SalaryRange,
                    ApplicationDeadline = createDto.ApplicationDeadline,
                    IsActive = createDto.IsActive,
                    CategoryId = createDto.CategoryId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.Positions.Add(position);
                await _context.SaveChangesAsync();

                // Add skills if provided
                if (createDto.SkillIds.Any())
                {
                    var positionSkills = createDto.SkillIds.Select(skillId => new PositionSkill
                    {
                        PositionId = position.PositionId,
                        SkillId = skillId
                    }).ToList();

                    _context.PositionSkills.AddRange(positionSkills);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Position created successfully: {PositionId}", position.PositionId);

                // Create history record for position creation
                await CreatePositionHistoryAsync(position.PositionId, "Position Created", null, $"Position '{position.Title}' created", createdBy, "Initial position creation");

                // Send notifications for new position
                await SendNewPositionNotificationsAsync(position.PositionId, position.Title, createdBy);

                var positionDetail = await GetPositionByIdAsync(position.PositionId);
                return (true, "Tạo vị trí thành công", positionDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position: {Title}", createDto.Title);
                return (false, "Có lỗi xảy ra khi tạo vị trí", null);
            }
        }

        public async Task<(bool Success, string Message, PositionDetailDto? Position)> UpdatePositionAsync(int positionId, UpdatePositionDto updateDto, int userId)
        {
            try
            {
                if (!await CanUserManagePositionAsync(positionId, userId))
                {
                    return (false, "Bạn không có quyền chỉnh sửa vị trí này", null);
                }

                var position = await _context.Positions
                    .Include(p => p.PositionSkills)
                        .ThenInclude(ps => ps.Skill)
                    .FirstOrDefaultAsync(p => p.PositionId == positionId);

                if (position == null)
                {
                    return (false, "Không tìm thấy vị trí", null);
                }

                // Track changes for history
                var changes = new List<PositionChangeTracker>
                {
                    new PositionChangeTracker { FieldName = "Title", OldValue = position.Title, NewValue = updateDto.Title },
                    new PositionChangeTracker { FieldName = "Description", OldValue = position.Description, NewValue = updateDto.Description },
                    new PositionChangeTracker { FieldName = "PositionType", OldValue = position.PositionType, NewValue = updateDto.PositionType },
                    new PositionChangeTracker { FieldName = "Location", OldValue = position.Location, NewValue = updateDto.Location },
                    new PositionChangeTracker { FieldName = "IsRemote", OldValue = position.IsRemote?.ToString(), NewValue = updateDto.IsRemote.ToString() },
                    new PositionChangeTracker { FieldName = "SalaryRange", OldValue = position.SalaryRange, NewValue = updateDto.SalaryRange },
                    new PositionChangeTracker { FieldName = "ApplicationDeadline", OldValue = position.ApplicationDeadline?.ToString("yyyy-MM-dd"), NewValue = updateDto.ApplicationDeadline?.ToString("yyyy-MM-dd") },
                    new PositionChangeTracker { FieldName = "CategoryId", OldValue = position.CategoryId?.ToString(), NewValue = updateDto.CategoryId?.ToString() },
                    new PositionChangeTracker { FieldName = "IsActive", OldValue = position.IsActive?.ToString(), NewValue = updateDto.IsActive.ToString() }
                };

                // Track skill changes
                var oldSkillNames = position.PositionSkills.Select(ps => ps.Skill.Name).OrderBy(x => x).ToList();
                var newSkillNames = new List<string>();

                if (updateDto.SkillIds.Any())
                {
                    var skills = await _context.Skills.Where(s => updateDto.SkillIds.Contains(s.SkillId)).ToListAsync();
                    newSkillNames = skills.Select(s => s.Name).OrderBy(x => x).ToList();
                }

                changes.Add(new PositionChangeTracker
                {
                    FieldName = "Skills",
                    OldValue = string.Join(", ", oldSkillNames),
                    NewValue = string.Join(", ", newSkillNames)
                });

                // Update position
                position.Title = updateDto.Title;
                position.Description = updateDto.Description;
                position.PositionType = updateDto.PositionType;
                position.Location = updateDto.Location;
                position.IsRemote = updateDto.IsRemote;
                position.SalaryRange = updateDto.SalaryRange;
                position.ApplicationDeadline = updateDto.ApplicationDeadline;
                position.CategoryId = updateDto.CategoryId;
                position.IsActive = updateDto.IsActive;

                // Update skills
                _context.PositionSkills.RemoveRange(position.PositionSkills);

                if (updateDto.SkillIds.Any())
                {
                    var newPositionSkills = updateDto.SkillIds.Select(skillId => new PositionSkill
                    {
                        PositionId = positionId,
                        SkillId = skillId
                    }).ToList();

                    _context.PositionSkills.AddRange(newPositionSkills);
                }

                await _context.SaveChangesAsync();

                // Create history records for changed fields
                foreach (var change in changes.Where(c => c.HasChanged))
                {
                    await CreatePositionHistoryAsync(positionId, $"Updated {change.FieldName}", change.OldValue, change.NewValue, userId);
                }

                // Send notifications for position updates
                await SendPositionUpdateNotificationsAsync(positionId, position.Title, changes.Where(c => c.HasChanged).ToList(), userId);

                var positionDetail = await GetPositionByIdAsync(positionId);
                return (true, "Cập nhật vị trí thành công", positionDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position: {PositionId}", positionId);
                return (false, "Có lỗi xảy ra khi cập nhật vị trí", null);
            }
        }

        public async Task<(bool Success, string Message)> DeletePositionAsync(int positionId, int userId)
        {
            try
            {
                if (!await CanUserManagePositionAsync(positionId, userId))
                {
                    return (false, "Bạn không có quyền xóa vị trí này");
                }

                var position = await _context.Positions
                    .Include(p => p.Applications)
                    .FirstOrDefaultAsync(p => p.PositionId == positionId);

                if (position == null)
                {
                    return (false, "Không tìm thấy vị trí");
                }

                if (position.Applications.Any())
                {
                    return (false, "Không thể xóa vị trí đã có đơn ứng tuyển");
                }

                // Send notifications before deleting
                await SendPositionDeletedNotificationsAsync(positionId, position.Title, userId);

                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                return (true, "Xóa vị trí thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting position: {PositionId}", positionId);
                return (false, "Có lỗi xảy ra khi xóa vị trí");
            }
        }

        public async Task<PositionDetailDto?> GetPositionByIdAsync(int positionId)
        {
            try
            {
                var position = await _context.Positions
                    .Include(p => p.Company)
                    .Include(p => p.Category)
                    .Include(p => p.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(p => p.PositionSkills)
                        .ThenInclude(ps => ps.Skill)
                    .Include(p => p.Applications)
                        .ThenInclude(a => a.User)
                            .ThenInclude(u => u.UserProfile)
                    .FirstOrDefaultAsync(p => p.PositionId == positionId);

                if (position == null) return null;

                return new PositionDetailDto
                {
                    PositionId = position.PositionId,
                    Title = position.Title,
                    Description = position.Description,
                    PositionType = position.PositionType,
                    Location = position.Location,
                    IsRemote = position.IsRemote,
                    SalaryRange = position.SalaryRange,
                    ApplicationDeadline = position.ApplicationDeadline,
                    IsActive = position.IsActive,
                    CreatedAt = position.CreatedAt,
                    CreatedBy = position.CreatedBy,
                    CreatedByName = position.CreatedByNavigation?.UserProfile != null 
                        ? $"{position.CreatedByNavigation.UserProfile.FirstName} {position.CreatedByNavigation.UserProfile.LastName}".Trim()
                        : null,

                    CompanyId = position.Company.CompanyId,
                    CompanyName = position.Company.Name,
                    CompanyDescription = position.Company.Description,
                    CompanyLogoUrl = position.Company.LogoUrl,
                    CompanyWebsite = position.Company.Website,
                    CompanyIndustry = position.Company.Industry,
                    CompanyLocation = position.Company.Location,

                    CategoryId = position.Category?.CategoryId,
                    CategoryName = position.Category?.CategoryName,
                    CategoryDescription = position.Category?.Description,

                    RequiredSkills = position.PositionSkills.Select(ps => new PositionSkillDto
                    {
                        SkillId = ps.Skill.SkillId,
                        SkillName = ps.Skill.Name,
                        SkillCategory = ps.Skill.Category
                    }).ToList(),

                    ApplicationCount = position.Applications.Count,
                    RecentApplications = position.Applications
                        .OrderByDescending(a => a.AppliedAt)
                        .Take(5)
                        .Select(a => new PositionApplicationDto
                        {
                            ApplicationId = a.ApplicationId,
                            UserId = a.UserId,
                            ApplicantName = a.User.UserProfile != null 
                                ? $"{a.User.UserProfile.FirstName} {a.User.UserProfile.LastName}".Trim()
                                : a.User.Email,
                            ApplicantEmail = a.User.Email,
                            ApplicantPhone = a.User.UserProfile?.Phone,
                            CurrentStatus = a.CurrentStatus,
                            AppliedAt = a.AppliedAt,
                            CoverLetter = a.CoverLetter
                        }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting position by ID: {PositionId}", positionId);
                return null;
            }
        }

        // Additional required methods
        public async Task<PositionSearchResultDto> GetPositionsAsync(PositionSearchDto searchDto)
        {
            try
            {
                var query = _context.Positions
                    .Include(p => p.Company)
                    .Include(p => p.Category)
                    .Include(p => p.Applications)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    query = query.Where(p => p.Title.Contains(searchDto.SearchTerm) ||
                                           p.Description.Contains(searchDto.SearchTerm) ||
                                           p.Company.Name.Contains(searchDto.SearchTerm));
                }

                if (searchDto.CompanyId.HasValue)
                {
                    query = query.Where(p => p.CompanyId == searchDto.CompanyId.Value);
                }

                if (searchDto.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == searchDto.IsActive.Value);
                }

                query = searchDto.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt);

                var totalCount = await query.CountAsync();

                var positions = await query
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .Select(p => new PositionListDto
                    {
                        PositionId = p.PositionId,
                        Title = p.Title,
                        CompanyName = p.Company.Name,
                        CompanyLogoUrl = p.Company.LogoUrl,
                        PositionType = p.PositionType,
                        Location = p.Location,
                        IsRemote = p.IsRemote,
                        SalaryRange = p.SalaryRange,
                        ApplicationDeadline = p.ApplicationDeadline,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        CategoryName = p.Category != null ? p.Category.CategoryName : null,
                        ApplicationCount = p.Applications.Count
                    })
                    .ToListAsync();

                return new PositionSearchResultDto
                {
                    Positions = positions,
                    TotalCount = totalCount,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching positions");
                return new PositionSearchResultDto();
            }
        }

        public async Task<List<PositionListDto>> GetPositionsByCompanyAsync(int companyId)
        {
            return await _context.Positions
                .Include(p => p.Company)
                .Include(p => p.Applications)
                .Where(p => p.CompanyId == companyId)
                .Select(p => new PositionListDto
                {
                    PositionId = p.PositionId,
                    Title = p.Title,
                    CompanyName = p.Company.Name,
                    PositionType = p.PositionType,
                    Location = p.Location,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    ApplicationCount = p.Applications.Count
                })
                .ToListAsync();
        }

        public async Task<List<PositionListDto>> GetActivePositionsAsync()
        {
            return await _context.Positions
                .Include(p => p.Company)
                .Include(p => p.Applications)
                .Where(p => p.IsActive == true)
                .Select(p => new PositionListDto
                {
                    PositionId = p.PositionId,
                    Title = p.Title,
                    CompanyName = p.Company.Name,
                    PositionType = p.PositionType,
                    Location = p.Location,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    ApplicationCount = p.Applications.Count
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> UpdatePositionStatusAsync(int positionId, bool isActive, int userId)
        {
            try
            {
                if (!await CanUserManagePositionAsync(positionId, userId))
                {
                    return (false, "Bạn không có quyền thay đổi trạng thái vị trí này");
                }

                var position = await _context.Positions.FindAsync(positionId);
                if (position == null)
                {
                    return (false, "Không tìm thấy vị trí");
                }

                var oldStatus = position.IsActive;
                position.IsActive = isActive;
                await _context.SaveChangesAsync();

                // Send notifications for status change
                await SendPositionStatusChangeNotificationsAsync(positionId, position.Title, oldStatus, isActive, userId);

                return (true, $"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} vị trí thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position status: {PositionId}", positionId);
                return (false, "Có lỗi xảy ra khi cập nhật trạng thái");
            }
        }

        public async Task<(bool Success, string Message)> ExtendDeadlineAsync(int positionId, DateOnly newDeadline, int userId)
        {
            try
            {
                if (!await CanUserManagePositionAsync(positionId, userId))
                {
                    return (false, "Bạn không có quyền gia hạn vị trí này");
                }

                var position = await _context.Positions.FindAsync(positionId);
                if (position == null)
                {
                    return (false, "Không tìm thấy vị trí");
                }

                position.ApplicationDeadline = newDeadline;
                await _context.SaveChangesAsync();

                return (true, "Gia hạn thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending deadline: {PositionId}", positionId);
                return (false, "Có lỗi xảy ra khi gia hạn");
            }
        }

        // Helper and utility methods
        public async Task<bool> CanUserManagePositionAsync(int positionId, int userId)
        {
            var position = await _context.Positions
                .Include(p => p.Company)
                    .ThenInclude(c => c.CompanyRecruiters)
                .FirstOrDefaultAsync(p => p.PositionId == positionId);

            if (position == null) return false;

            // User can manage if they created the position
            if (position.CreatedBy == userId) return true;

            // User can manage if they are an approved recruiter for the company
            return position.Company.CompanyRecruiters.Any(cr => cr.UserId == userId && cr.IsApproved == true);
        }

        public async Task<List<PositionApplicationDto>> GetPositionApplicationsAsync(int positionId, int userId)
        {
            if (!await CanUserManagePositionAsync(positionId, userId))
            {
                return new List<PositionApplicationDto>();
            }

            return await _context.Applications
                .Include(a => a.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(a => a.PositionId == positionId)
                .Select(a => new PositionApplicationDto
                {
                    ApplicationId = a.ApplicationId,
                    UserId = a.UserId,
                    ApplicantName = a.User.UserProfile != null
                        ? $"{a.User.UserProfile.FirstName} {a.User.UserProfile.LastName}".Trim()
                        : a.User.Email,
                    ApplicantEmail = a.User.Email,
                    CurrentStatus = a.CurrentStatus,
                    AppliedAt = a.AppliedAt
                })
                .ToListAsync();
        }

        public async Task<int> GetApplicationCountAsync(int positionId)
        {
            return await _context.Applications.CountAsync(a => a.PositionId == positionId);
        }

        public async Task<bool> IsPositionActiveAsync(int positionId)
        {
            var position = await _context.Positions.FindAsync(positionId);
            return position?.IsActive == true;
        }

        public async Task<bool> IsDeadlineValidAsync(DateOnly? deadline)
        {
            return !deadline.HasValue || deadline.Value > DateOnly.FromDateTime(DateTime.Now);
        }

        public async Task<List<JobCategory>> GetJobCategoriesAsync()
        {
            return await _context.JobCategories.OrderBy(c => c.CategoryName).ToListAsync();
        }

        public async Task<List<Company>> GetUserCompaniesAsync(int userId)
        {
            // Get companies where user is owner or approved recruiter
            var ownedCompanies = await _context.Companies
                .Where(c => c.CreatedBy == userId)
                .ToListAsync();

            var recruiterCompanies = await _context.CompanyRecruiters
                .Include(cr => cr.Company)
                .Where(cr => cr.UserId == userId && cr.IsApproved == true && cr.Status == "approved")
                .Select(cr => cr.Company)
                .ToListAsync();

            // Combine and remove duplicates
            var allCompanies = ownedCompanies.Concat(recruiterCompanies)
                .GroupBy(c => c.CompanyId)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();

            return allCompanies;
        }

        public async Task<List<Skill>> GetSkillsAsync()
        {
            return await _context.Skills.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<List<Skill>> GetSkillsByCategoryAsync(string category)
        {
            return await _context.Skills
                .Where(s => s.Category == category)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<PositionStatisticsDto> GetPositionStatisticsAsync(int userId)
        {
            var positions = await _context.Positions
                .Include(p => p.Applications)
                .Where(p => p.CreatedBy == userId)
                .ToListAsync();

            var totalApplications = positions.SelectMany(p => p.Applications).Count();

            return new PositionStatisticsDto
            {
                TotalPositions = positions.Count,
                ActivePositions = positions.Count(p => p.IsActive == true),
                InactivePositions = positions.Count(p => p.IsActive == false),
                ExpiredPositions = positions.Count(p => p.ApplicationDeadline.HasValue &&
                    p.ApplicationDeadline.Value < DateOnly.FromDateTime(DateTime.Now)),
                TotalApplications = totalApplications,
                AverageApplicationsPerPosition = positions.Count > 0 ? (double)totalApplications / positions.Count : 0
            };
        }

        public async Task<PositionStatisticsDto> GetCompanyPositionStatisticsAsync(int companyId, int userId)
        {
            var positions = await _context.Positions
                .Include(p => p.Applications)
                .Where(p => p.CompanyId == companyId && p.CreatedBy == userId)
                .ToListAsync();

            var totalApplications = positions.SelectMany(p => p.Applications).Count();

            return new PositionStatisticsDto
            {
                TotalPositions = positions.Count,
                ActivePositions = positions.Count(p => p.IsActive == true),
                InactivePositions = positions.Count(p => p.IsActive == false),
                TotalApplications = totalApplications,
                AverageApplicationsPerPosition = positions.Count > 0 ? (double)totalApplications / positions.Count : 0
            };
        }
        // History Tracking Methods
        public async Task<List<PositionHistoryDto>> GetPositionHistoryAsync(int positionId, int userId)
        {
            try
            {
                if (!await CanUserManagePositionAsync(positionId, userId))
                {
                    return new List<PositionHistoryDto>();
                }

                return await _context.PositionHistories
                    .Include(ph => ph.ChangedByUser)
                        .ThenInclude(u => u.UserProfile)
                    .Where(ph => ph.PositionId == positionId)
                    .OrderByDescending(ph => ph.ChangedAt)
                    .Select(ph => new PositionHistoryDto
                    {
                        HistoryId = ph.HistoryId,
                        PositionId = ph.PositionId,
                        ChangedByUserId = ph.ChangedByUserId,
                        ChangedByUserName = ph.ChangedByUser != null && ph.ChangedByUser.UserProfile != null
                            ? $"{ph.ChangedByUser.UserProfile.FirstName} {ph.ChangedByUser.UserProfile.LastName}".Trim()
                            : ph.ChangedByUser != null ? ph.ChangedByUser.Email : "Unknown",
                        ChangedAt = ph.ChangedAt,
                        ChangeType = ph.ChangeType,
                        OldValue = ph.OldValue,
                        NewValue = ph.NewValue,
                        Notes = ph.Notes
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting position history: {PositionId}", positionId);
                return new List<PositionHistoryDto>();
            }
        }

        public async Task CreatePositionHistoryAsync(int positionId, string changeType, string? oldValue, string? newValue, int changedByUserId, string? notes = null)
        {
            try
            {
                var history = new PositionHistory
                {
                    PositionId = positionId,
                    ChangedByUserId = changedByUserId,
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangeType = changeType,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Notes = notes
                };

                _context.PositionHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Position history created: {PositionId}, {ChangeType}", positionId, changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position history: {PositionId}, {ChangeType}", positionId, changeType);
            }
        }

        public async Task<List<PositionListDto>> GetPositionsByUserAsync(int userId)
        {
            return await _context.Positions
                .Include(p => p.Company)
                .Include(p => p.Applications)
                .Where(p => p.CreatedBy == userId)
                .Select(p => new PositionListDto
                {
                    PositionId = p.PositionId,
                    Title = p.Title,
                    CompanyName = p.Company.Name,
                    PositionType = p.PositionType,
                    Location = p.Location,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    ApplicationCount = p.Applications.Count
                })
                .ToListAsync();
        }

        // Candidate Matching Methods for Notifications
        public async Task<List<int>> FindMatchingCandidatesAsync(int positionId)
        {
            try
            {
                _logger.LogInformation("Finding matching candidates for position: {PositionId}", positionId);

                var position = await _context.Positions
                    .Include(p => p.PositionSkills)
                    .FirstOrDefaultAsync(p => p.PositionId == positionId);

                if (position == null)
                {
                    return new List<int>();
                }

                var matchingCandidates = new HashSet<int>();

                // Find candidates by skills
                if (position.PositionSkills.Any())
                {
                    var skillIds = position.PositionSkills.Select(ps => ps.SkillId).ToList();
                    var candidatesBySkills = await FindCandidatesBySkillsAsync(skillIds);
                    foreach (var candidateId in candidatesBySkills)
                    {
                        matchingCandidates.Add(candidateId);
                    }
                }

                // Find candidates by location (if specified)
                if (!string.IsNullOrEmpty(position.Location))
                {
                    var candidatesByLocation = await FindCandidatesByLocationAsync(position.Location);
                    foreach (var candidateId in candidatesByLocation)
                    {
                        matchingCandidates.Add(candidateId);
                    }
                }

                // Filter out candidates who already applied
                var existingApplicants = await _context.Applications
                    .Where(a => a.PositionId == positionId)
                    .Select(a => a.UserId)
                    .ToListAsync();

                var finalCandidates = matchingCandidates
                    .Where(c => !existingApplicants.Contains(c))
                    .ToList();

                _logger.LogInformation("Found {Count} matching candidates for position {PositionId}",
                    finalCandidates.Count, positionId);

                return finalCandidates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding matching candidates for position: {PositionId}", positionId);
                return new List<int>();
            }
        }

        public async Task<List<int>> FindCandidatesBySkillsAsync(List<int> skillIds)
        {
            try
            {
                if (!skillIds.Any())
                {
                    return new List<int>();
                }

                // Find users who have at least one of the required skills and are candidates
                var candidates = await _context.UserSkills
                    .Where(us => skillIds.Contains(us.SkillId))
                    .Include(us => us.User)
                        .ThenInclude(u => u.Role)
                    .Where(us => us.User.Role.RoleName == "Candidate" && us.User.IsActive == true)
                    .Select(us => us.UserId)
                    .Distinct()
                    .ToListAsync();

                return candidates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding candidates by skills");
                return new List<int>();
            }
        }

        public async Task<List<int>> FindCandidatesByLocationAsync(string location)
        {
            try
            {
                if (string.IsNullOrEmpty(location))
                {
                    return new List<int>();
                }

                // Find candidates whose profile location matches or contains the position location
                var candidates = await _context.UserProfiles
                    .Include(up => up.User)
                        .ThenInclude(u => u.Role)
                    .Where(up => up.User.Role.RoleName == "Candidate"
                              && up.User.IsActive == true
                              && up.Address != null
                              && (up.Address.Contains(location) || location.Contains(up.Address)))
                    .Select(up => up.UserId)
                    .ToListAsync();

                return candidates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding candidates by location: {Location}", location);
                return new List<int>();
            }
        }

        public async Task<List<int>> GetPositionApplicantUserIdsAsync(int positionId)
        {
            try
            {
                var applicantIds = await _context.Applications
                    .Where(a => a.PositionId == positionId)
                    .Select(a => a.UserId)
                    .ToListAsync();

                return applicantIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicant user IDs for position: {PositionId}", positionId);
                return new List<int>();
            }
        }

        // Notification Methods
        private async Task SendNewPositionNotificationsAsync(int positionId, string jobTitle, int createdBy)
        {
            try
            {
                _logger.LogInformation("Sending new position notifications for position: {PositionId}", positionId);

                // Find matching candidates and notify them
                var matchingCandidates = await FindMatchingCandidatesAsync(positionId);

                foreach (var candidateId in matchingCandidates.Take(50)) // Limit to 50 to avoid spam
                {
                    await _notificationIntegration.NotifyNewJobMatchingCriteriaAsync(
                        candidateId,
                        jobTitle,
                        positionId,
                        "Phù hợp với kỹ năng và vị trí của bạn"
                    );
                }

                _logger.LogInformation("Sent new position notifications to {Count} candidates for position {PositionId}",
                    matchingCandidates.Count, positionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new position notifications for position: {PositionId}", positionId);
                // Don't throw - notifications shouldn't break core functionality
            }
        }

        private async Task SendPositionUpdateNotificationsAsync(int positionId, string jobTitle, List<PositionChangeTracker> changes, int updatedBy)
        {
            try
            {
                _logger.LogInformation("Sending position update notifications for position: {PositionId}", positionId);

                // Get all applicants for this position
                var applicantIds = await GetPositionApplicantUserIdsAsync(positionId);

                // Check if important fields changed (deadline, status, location, etc.)
                var importantChanges = changes.Where(c =>
                    c.FieldName == "ApplicationDeadline" ||
                    c.FieldName == "IsActive" ||
                    c.FieldName == "Location" ||
                    c.FieldName == "SalaryRange").ToList();

                if (importantChanges.Any())
                {
                    foreach (var applicantId in applicantIds)
                    {
                        var changeDescription = string.Join(", ", importantChanges.Select(c => c.FieldName));
                        await _notificationIntegration.NotifyJobPostingApprovedAsync(
                            applicantId,
                            jobTitle,
                            positionId
                        );
                    }

                    _logger.LogInformation("Sent position update notifications to {Count} applicants for position {PositionId}",
                        applicantIds.Count, positionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending position update notifications for position: {PositionId}", positionId);
            }
        }

        private async Task SendPositionStatusChangeNotificationsAsync(int positionId, string jobTitle, bool? oldStatus, bool newStatus, int updatedBy)
        {
            try
            {
                if (oldStatus == newStatus) return;

                _logger.LogInformation("Sending position status change notifications for position: {PositionId}", positionId);

                // Get all applicants for this position
                var applicantIds = await GetPositionApplicantUserIdsAsync(positionId);

                string statusMessage = newStatus ? "đã được kích hoạt lại" : "đã bị tạm dừng";

                foreach (var applicantId in applicantIds)
                {
                    await _notificationIntegration.NotifyJobPostingExpiredAsync(
                        applicantId,
                        jobTitle,
                        positionId
                    );
                }

                _logger.LogInformation("Sent status change notifications to {Count} applicants for position {PositionId}",
                    applicantIds.Count, positionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending position status change notifications for position: {PositionId}", positionId);
            }
        }

        private async Task SendPositionDeletedNotificationsAsync(int positionId, string jobTitle, int deletedBy)
        {
            try
            {
                _logger.LogInformation("Sending position deleted notifications for position: {PositionId}", positionId);

                // Get all applicants for this position before deletion
                var applicantIds = await GetPositionApplicantUserIdsAsync(positionId);

                foreach (var applicantId in applicantIds)
                {
                    await _notificationIntegration.NotifyJobPostingExpiredAsync(
                        applicantId,
                        jobTitle,
                        positionId
                    );
                }

                _logger.LogInformation("Sent deletion notifications to {Count} applicants for position {PositionId}",
                    applicantIds.Count, positionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending position deleted notifications for position: {PositionId}", positionId);
            }
        }
    }

    // Helper class for tracking position changes
    public class PositionChangeTracker
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool HasChanged => OldValue != NewValue;
    }
}
