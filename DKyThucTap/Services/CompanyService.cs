using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Models.DTOs.Company;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DKyThucTap.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<CompanyService> _logger;
        private readonly INotificationIntegrationService _notificationIntegration;

        public CompanyService(
            DKyThucTapContext context,
            ILogger<CompanyService> logger,
            INotificationIntegrationService notificationIntegration)
        {
            _context = context;
            _logger = logger;
            _notificationIntegration = notificationIntegration;
        }

        public async Task<(bool Success, string Message, CompanyDetailDto? Company)> CreateCompanyAsync(CreateCompanyDto createDto, int createdBy)
        {
            try
            {
                _logger.LogInformation("Creating company: {Name} by user: {UserId}", createDto.Name, createdBy);

                // Check if company name already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == createDto.Name.ToLower());

                if (existingCompany != null)
                {
                    return (false, "Tên công ty đã tồn tại", null);
                }

                // Create company
                var company = new Company
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    LogoUrl = createDto.LogoUrl,
                    Website = createDto.Website,
                    Industry = createDto.Industry,
                    Location = createDto.Location,
                    CreatedBy = createdBy,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // Add creator as company owner
                var companyRecruiter = new CompanyRecruiter
                {
                    CompanyId = company.CompanyId,
                    UserId = createdBy,
                    RoleInCompany = "Chủ sở hữu",
                    IsAdmin = true,
                    IsApproved = true,
                    JoinedAt = DateTimeOffset.UtcNow,
                    AssignedAt = DateTimeOffset.UtcNow,
                    Status = "approved",
                    LastActivity = DateTimeOffset.UtcNow
                };

                _context.CompanyRecruiters.Add(companyRecruiter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company created successfully: {CompanyId}", company.CompanyId);

                // Send notifications for new company creation
                await SendNewCompanyNotificationsAsync(company.CompanyId, company.Name, createdBy);

                var companyDetail = await GetCompanyByIdAsync(company.CompanyId, createdBy);
                return (true, "Tạo công ty thành công", companyDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company: {Name}", createDto.Name);
                return (false, "Có lỗi xảy ra khi tạo công ty", null);
            }
        }

        public async Task<(bool Success, string Message, CompanyDetailDto? Company)> UpdateCompanyAsync(int companyId, UpdateCompanyDto updateDto, int userId)
        {
            try
            {
                _logger.LogInformation("Updating company: {CompanyId} by user: {UserId}", companyId, userId);

                if (!await CanUserManageCompanyAsync(companyId, userId))
                {
                    return (false, "Bạn không có quyền chỉnh sửa công ty này", null);
                }

                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return (false, "Không tìm thấy công ty", null);
                }

                // Check if new name conflicts with existing companies (excluding current)
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == updateDto.Name.ToLower() && c.CompanyId != companyId);

                if (existingCompany != null)
                {
                    return (false, "Tên công ty đã tồn tại", null);
                }

                // Update company
                company.Name = updateDto.Name;
                company.Description = updateDto.Description;
                company.LogoUrl = updateDto.LogoUrl;
                company.Website = updateDto.Website;
                company.Industry = updateDto.Industry;
                company.Location = updateDto.Location;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company updated successfully: {CompanyId}", companyId);

                // Send notifications for company profile update
                await SendCompanyUpdateNotificationsAsync(companyId, company.Name, userId);

                var companyDetail = await GetCompanyByIdAsync(companyId, userId);
                return (true, "Cập nhật công ty thành công", companyDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", companyId);
                return (false, "Có lỗi xảy ra khi cập nhật công ty", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteCompanyAsync(int companyId, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting company: {CompanyId} by user: {UserId}", companyId, userId);

                if (!await IsUserCompanyOwnerAsync(companyId, userId))
                {
                    return (false, "Chỉ chủ sở hữu công ty mới có thể xóa công ty");
                }

                var company = await _context.Companies
                    .Include(c => c.Positions)
                    .Include(c => c.CompanyRecruiters)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return (false, "Không tìm thấy công ty");
                }

                // Check if company has positions with applications
                var hasApplications = await _context.Applications
                    .AnyAsync(a => a.Position.CompanyId == companyId);

                if (hasApplications)
                {
                    return (false, "Không thể xóa công ty đã có đơn ứng tuyển. Bạn có thể vô hiệu hóa công ty thay thế.");
                }

                // Remove all company recruiters
                _context.CompanyRecruiters.RemoveRange(company.CompanyRecruiters);
                
                // Remove all positions (if no applications)
                _context.Positions.RemoveRange(company.Positions);
                
                // Remove company
                _context.Companies.Remove(company);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company deleted successfully: {CompanyId}", companyId);
                return (true, "Xóa công ty thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company: {CompanyId}", companyId);
                return (false, "Có lỗi xảy ra khi xóa công ty");
            }
        }

        public async Task<CompanyDetailDto?> GetCompanyByIdAsync(int companyId, int? userId = null)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.CompanyRecruiters)
                        .ThenInclude(cr => cr.User)
                            .ThenInclude(u => u.UserProfile)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return null;
                }

                var userRole = userId.HasValue ? await GetUserRoleInCompanyAsync(companyId, userId.Value) : null;
                var canManage = userId.HasValue && await CanUserManageCompanyAsync(companyId, userId.Value);

                return new CompanyDetailDto
                {
                    CompanyId = company.CompanyId,
                    Name = company.Name,
                    Description = company.Description,
                    LogoUrl = company.LogoUrl,
                    Website = company.Website,
                    Industry = company.Industry,
                    Location = company.Location,
                    CreatedAt = company.CreatedAt,
                    CreatedBy = company.CreatedBy,
                    CreatedByName = company.CreatedByNavigation?.UserProfile != null
                        ? $"{company.CreatedByNavigation.UserProfile.FirstName} {company.CreatedByNavigation.UserProfile.LastName}".Trim()
                        : company.CreatedByNavigation?.Email,

                    // Statistics
                    PositionCount = company.Positions.Count,
                    ActivePositionCount = company.Positions.Count(p => p.IsActive == true),
                    TotalApplications = company.Positions.SelectMany(p => p.Applications).Count(),
                    RecruiterCount = company.CompanyRecruiters.Count(cr => cr.IsApproved == true),

                    // User relationship
                    UserRole = userRole,
                    CanManage = canManage,

                    // Recent positions
                    RecentPositions = company.Positions
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .Select(p => new CompanyPositionDto
                        {
                            PositionId = p.PositionId,
                            Title = p.Title,
                            PositionType = p.PositionType,
                            IsActive = p.IsActive,
                            CreatedAt = p.CreatedAt,
                            ApplicationCount = p.Applications.Count
                        }).ToList(),

                    // Recruiters
                    Recruiters = company.CompanyRecruiters
                        .Where(cr => cr.IsApproved == true)
                        .Select(cr => new CompanyRecruiterDto
                        {
                            UserId = cr.UserId,
                            Name = cr.User.UserProfile != null
                                ? $"{cr.User.UserProfile.FirstName} {cr.User.UserProfile.LastName}".Trim()
                                : cr.User.Email,
                            Email = cr.User.Email,
                            JoinedAt = cr.JoinedAt,
                            IsApproved = cr.IsApproved ?? false,
                            Status = cr.UserId == company.CreatedBy ? "Owner" : "Active",
                            PositionCount = company.Positions.Count(p => p.CreatedBy == cr.UserId)
                        }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company by ID: {CompanyId}", companyId);
                return null;
            }
        }

        // Authorization & Validation Methods
        public async Task<bool> CanUserManageCompanyAsync(int companyId, int userId)
        {
            try
            {
                var companyRecruiter = await _context.CompanyRecruiters
                    .Include(cr => cr.Company)
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == userId && cr.IsApproved == true);

                return companyRecruiter != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user company management permission: {CompanyId}, {UserId}", companyId, userId);
                return false;
            }
        }

        public async Task<bool> IsUserCompanyRecruiterAsync(int companyId, int userId)
        {
            try
            {
                return await _context.CompanyRecruiters
                    .AnyAsync(cr => cr.CompanyId == companyId && cr.UserId == userId && cr.IsApproved == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user company recruiter status: {CompanyId}, {UserId}", companyId, userId);
                return false;
            }
        }

        public async Task<bool> IsUserCompanyOwnerAsync(int companyId, int userId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                return company?.CreatedBy == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user company owner status: {CompanyId}, {UserId}", companyId, userId);
                return false;
            }
        }

        public async Task<string> GetUserRoleInCompanyAsync(int companyId, int userId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company?.CreatedBy == userId)
                {
                    return "Owner";
                }

                var companyRecruiter = await _context.CompanyRecruiters
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == userId);

                if (companyRecruiter == null)
                {
                    return "None";
                }

                return companyRecruiter.IsApproved == true ? "Recruiter" : "Pending";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role in company: {CompanyId}, {UserId}", companyId, userId);
                return "None";
            }
        }

        public async Task<List<CompanyListDto>> GetAllCompaniesAsync(int? userId = null)
        {
            try
            {
                var companies = await _context.Companies
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.CompanyRecruiters)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var result = new List<CompanyListDto>();

                foreach (var company in companies)
                {
                    var userRole = userId.HasValue ? await GetUserRoleInCompanyAsync(company.CompanyId, userId.Value) : null;
                    var companyRecruiter = userId.HasValue
                        ? company.CompanyRecruiters.FirstOrDefault(cr => cr.UserId == userId.Value)
                        : null;

                    result.Add(new CompanyListDto
                    {
                        CompanyId = company.CompanyId,
                        Name = company.Name,
                        Description = company.Description,
                        LogoUrl = company.LogoUrl,
                        Website = company.Website,
                        Industry = company.Industry,
                        Location = company.Location,
                        CreatedAt = company.CreatedAt,
                        CreatedBy = company.CreatedBy,
                        CreatedByName = company.CreatedByNavigation?.UserProfile != null
                            ? $"{company.CreatedByNavigation.UserProfile.FirstName} {company.CreatedByNavigation.UserProfile.LastName}".Trim()
                            : company.CreatedByNavigation?.Email,

                        // Statistics
                        PositionCount = company.Positions.Count,
                        ActivePositionCount = company.Positions.Count(p => p.IsActive == true),
                        TotalApplications = company.Positions.SelectMany(p => p.Applications).Count(),
                        RecruiterCount = company.CompanyRecruiters.Count(cr => cr.IsApproved == true),

                        // User relationship
                        UserRole = userRole,
                        JoinedAt = companyRecruiter?.JoinedAt,
                        IsApproved = companyRecruiter?.IsApproved ?? false
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all companies");
                return new List<CompanyListDto>();
            }
        }

        public async Task<List<CompanyListDto>> SearchCompaniesAsync(string searchTerm, int? userId = null)
        {
            try
            {
                var query = _context.Companies
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.CompanyRecruiters)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Name.Contains(searchTerm) ||
                                           (c.Description != null && c.Description.Contains(searchTerm)) ||
                                           (c.Industry != null && c.Industry.Contains(searchTerm)) ||
                                           (c.Location != null && c.Location.Contains(searchTerm)));
                }

                var companies = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var result = new List<CompanyListDto>();

                foreach (var company in companies)
                {
                    var userRole = userId.HasValue ? await GetUserRoleInCompanyAsync(company.CompanyId, userId.Value) : null;
                    var companyRecruiter = userId.HasValue
                        ? company.CompanyRecruiters.FirstOrDefault(cr => cr.UserId == userId.Value)
                        : null;

                    result.Add(new CompanyListDto
                    {
                        CompanyId = company.CompanyId,
                        Name = company.Name,
                        Description = company.Description,
                        LogoUrl = company.LogoUrl,
                        Website = company.Website,
                        Industry = company.Industry,
                        Location = company.Location,
                        CreatedAt = company.CreatedAt,
                        CreatedBy = company.CreatedBy,
                        CreatedByName = company.CreatedByNavigation?.UserProfile != null
                            ? $"{company.CreatedByNavigation.UserProfile.FirstName} {company.CreatedByNavigation.UserProfile.LastName}".Trim()
                            : company.CreatedByNavigation?.Email,

                        // Statistics
                        PositionCount = company.Positions.Count,
                        ActivePositionCount = company.Positions.Count(p => p.IsActive == true),
                        TotalApplications = company.Positions.SelectMany(p => p.Applications).Count(),
                        RecruiterCount = company.CompanyRecruiters.Count(cr => cr.IsApproved == true),

                        // User relationship
                        UserRole = userRole,
                        JoinedAt = companyRecruiter?.JoinedAt,
                        IsApproved = companyRecruiter?.IsApproved ?? false
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching companies: {SearchTerm}", searchTerm);
                return new List<CompanyListDto>();
            }
        }

        public async Task<List<CompanyListDto>> GetUserCompaniesAsync(int userId)
        {
            try
            {
                var userCompanies = await _context.CompanyRecruiters
                    .Include(cr => cr.Company)
                        .ThenInclude(c => c.CreatedByNavigation)
                            .ThenInclude(u => u.UserProfile)
                    .Include(cr => cr.Company)
                        .ThenInclude(c => c.Positions)
                            .ThenInclude(p => p.Applications)
                    .Include(cr => cr.Company)
                        .ThenInclude(c => c.CompanyRecruiters)
                    .Where(cr => cr.UserId == userId && cr.IsApproved == true)
                    .OrderByDescending(cr => cr.JoinedAt)
                    .ToListAsync();

                return userCompanies.Select(cr => new CompanyListDto
                {
                    CompanyId = cr.Company.CompanyId,
                    Name = cr.Company.Name,
                    Description = cr.Company.Description,
                    LogoUrl = cr.Company.LogoUrl,
                    Website = cr.Company.Website,
                    Industry = cr.Company.Industry,
                    Location = cr.Company.Location,
                    CreatedAt = cr.Company.CreatedAt,
                    CreatedBy = cr.Company.CreatedBy,
                    CreatedByName = cr.Company.CreatedByNavigation?.UserProfile != null
                        ? $"{cr.Company.CreatedByNavigation.UserProfile.FirstName} {cr.Company.CreatedByNavigation.UserProfile.LastName}".Trim()
                        : cr.Company.CreatedByNavigation?.Email,

                    // Statistics
                    PositionCount = cr.Company.Positions.Count,
                    ActivePositionCount = cr.Company.Positions.Count(p => p.IsActive == true),
                    TotalApplications = cr.Company.Positions.SelectMany(p => p.Applications).Count(),
                    RecruiterCount = cr.Company.CompanyRecruiters.Count(recruiter => recruiter.IsApproved == true),

                    // User relationship
                    UserRole = cr.UserId == cr.Company.CreatedBy ? "Owner" : "Recruiter",
                    JoinedAt = cr.JoinedAt,
                    IsApproved = cr.IsApproved ?? false
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user companies: {UserId}", userId);
                return new List<CompanyListDto>();
            }
        }

        public async Task<(bool Success, string Message)> RequestToJoinCompanyAsync(int companyId, int userId, string? message = null)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting to join company {CompanyId}", userId, companyId);

                // Check if company exists
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return (false, "Không tìm thấy công ty");
                }

                // Check if user already has a relationship with this company
                var existingRelation = await _context.CompanyRecruiters
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == userId);

                if (existingRelation != null)
                {
                    if (existingRelation.IsApproved == true)
                    {
                        return (false, "Bạn đã là thành viên của công ty này");
                    }
                    else
                    {
                        return (false, "Yêu cầu tham gia của bạn đang chờ phê duyệt");
                    }
                }

                // Create join request
                var joinRequest = new CompanyRecruiter
                {
                    CompanyId = companyId,
                    UserId = userId,
                    RoleInCompany = "Nhân viên tuyển dụng",
                    IsAdmin = false,
                    IsApproved = false,
                    RequestMessage = message,
                    Status = "pending",
                    AssignedAt = DateTimeOffset.UtcNow,
                    LastActivity = DateTimeOffset.UtcNow
                };

                _context.CompanyRecruiters.Add(joinRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Join request created successfully for user {UserId} and company {CompanyId}", userId, companyId);
                return (true, "Yêu cầu tham gia đã được gửi và đang chờ phê duyệt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating join request for user {UserId} and company {CompanyId}", userId, companyId);
                return (false, "Có lỗi xảy ra khi gửi yêu cầu tham gia");
            }
        }

        public async Task<(bool Success, string Message)> InviteRecruiterAsync(int companyId, string userEmail, int invitedBy, string? message = null)
        {
            try
            {
                _logger.LogInformation("User {InvitedBy} inviting {UserEmail} to company {CompanyId}", invitedBy, userEmail, companyId);

                // Check if inviter can manage this company
                if (!await CanUserManageCompanyAsync(companyId, invitedBy))
                {
                    return (false, "Bạn không có quyền mời người khác vào công ty này");
                }

                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng với email này");
                }

                // Check if user already has a relationship with this company
                var existingRelation = await _context.CompanyRecruiters
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == user.UserId);

                if (existingRelation != null)
                {
                    if (existingRelation.IsApproved == true)
                    {
                        return (false, "Người dùng này đã là thành viên của công ty");
                    }
                    else
                    {
                        return (false, "Người dùng này đã có yêu cầu tham gia đang chờ phê duyệt");
                    }
                }

                // Create invitation
                var invitation = new CompanyRecruiter
                {
                    CompanyId = companyId,
                    UserId = user.UserId,
                    RoleInCompany = "Nhân viên tuyển dụng",
                    IsAdmin = false,
                    IsApproved = false,
                    RequestMessage = message,
                    InvitedBy = invitedBy,
                    Status = "invited",
                    AssignedAt = DateTimeOffset.UtcNow,
                    LastActivity = DateTimeOffset.UtcNow
                };

                _context.CompanyRecruiters.Add(invitation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invitation created successfully for user {UserId} to company {CompanyId}", user.UserId, companyId);
                return (true, "Lời mời đã được gửi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user {UserEmail} to company {CompanyId}", userEmail, companyId);
                return (false, "Có lỗi xảy ra khi gửi lời mời");
            }
        }

        public async Task<(bool Success, string Message)> RespondToRecruiterRequestAsync(int companyId, int userId, bool isApproved, int respondedBy, string? responseMessage = null)
        {
            try
            {
                _logger.LogInformation("User {RespondedBy} responding to request from {UserId} for company {CompanyId}: {IsApproved}",
                    respondedBy, userId, companyId, isApproved);

                // Find the request first to check its status
                var request = await _context.CompanyRecruiters
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == userId && cr.IsApproved == false);

                if (request == null)
                {
                    _logger.LogWarning("Request not found for company {CompanyId} and user {UserId}", companyId, userId);
                    return (false, "Không tìm thấy yêu cầu tham gia");
                }

                _logger.LogInformation("Found request with status: {Status}", request.Status);

                // Check permissions based on request type
                if (request.Status == "pending")
                {
                    // For pending requests (user requested to join), only company managers can respond
                    var canManage = await CanUserManageCompanyAsync(companyId, respondedBy);
                    if (!canManage)
                    {
                        return (false, "Chỉ quản lý công ty mới có thể phê duyệt yêu cầu tham gia");
                    }
                }
                else if (request.Status == "invited")
                {
                    // For invitations (company invited user), only the invited user can respond
                    if (userId != respondedBy)
                    {
                        return (false, "Chỉ người được mời mới có thể phản hồi lời mời");
                    }
                }
                else
                {
                    return (false, "Yêu cầu này không thể được xử lý");
                }

                // Update request
                request.IsApproved = isApproved;
                request.RespondedBy = respondedBy;
                request.RespondedAt = DateTimeOffset.UtcNow;
                request.ResponseMessage = responseMessage;
                request.Status = isApproved ? "approved" : "rejected";
                request.LastActivity = DateTimeOffset.UtcNow;

                if (isApproved)
                {
                    request.JoinedAt = DateTimeOffset.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Send notifications for approval/rejection
                await SendRecruiterRequestResponseNotificationsAsync(companyId, userId, isApproved, request.Status);

                var message = isApproved ? "Yêu cầu tham gia đã được phê duyệt" : "Yêu cầu tham gia đã bị từ chối";
                _logger.LogInformation("Request response completed successfully: {Message}", message);
                return (true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to request from user {UserId} for company {CompanyId}", userId, companyId);
                return (false, "Có lỗi xảy ra khi xử lý yêu cầu");
            }
        }

        public async Task<(bool Success, string Message)> RemoveRecruiterAsync(int companyId, int userId, int removedBy)
        {
            try
            {
                _logger.LogInformation("User {RemovedBy} removing user {UserId} from company {CompanyId}", removedBy, userId, companyId);

                // Check if remover can manage this company
                if (!await CanUserManageCompanyAsync(companyId, removedBy))
                {
                    return (false, "Bạn không có quyền xóa nhân viên khỏi công ty này");
                }

                // Check if target is the company owner
                var company = await _context.Companies.FindAsync(companyId);
                if (company?.CreatedBy == userId)
                {
                    return (false, "Không thể xóa chủ sở hữu công ty");
                }

                // Find the recruiter relationship
                var recruiter = await _context.CompanyRecruiters
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == userId);

                if (recruiter == null)
                {
                    return (false, "Không tìm thấy nhân viên trong công ty này");
                }

                // Remove the relationship
                _context.CompanyRecruiters.Remove(recruiter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recruiter {UserId} removed successfully from company {CompanyId}", userId, companyId);
                return (true, "Đã xóa nhân viên khỏi công ty thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing recruiter {UserId} from company {CompanyId}", userId, companyId);
                return (false, "Có lỗi xảy ra khi xóa nhân viên");
            }
        }

        public async Task<(bool Success, string Message)> LeaveCompanyAsync(int companyId, int userId)
        {
            try
            {
                _logger.LogInformation("User {UserId} leaving company {CompanyId}", userId, companyId);

                // Check if user is the company owner
                var company = await _context.Companies.FindAsync(companyId);
                if (company?.CreatedBy == userId)
                {
                    return (false, "Chủ sở hữu không thể rời khỏi công ty. Bạn có thể xóa công ty hoặc chuyển quyền sở hữu cho người khác.");
                }

                // Find the recruiter relationship
                var recruiter = await _context.CompanyRecruiters
                    .FirstOrDefaultAsync(cr => cr.CompanyId == companyId && cr.UserId == userId);

                if (recruiter == null)
                {
                    return (false, "Bạn không phải là thành viên của công ty này");
                }

                // Remove the relationship
                _context.CompanyRecruiters.Remove(recruiter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} left company {CompanyId} successfully", userId, companyId);
                return (true, "Bạn đã rời khỏi công ty thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving company {CompanyId} for user {UserId}", companyId, userId);
                return (false, "Có lỗi xảy ra khi rời khỏi công ty");
            }
        }

        public async Task<List<CompanyRecruiterListDto>> GetCompanyRecruitersAsync(int companyId, int userId)
        {
            try
            {
                // Check if user can view this company's recruiters
                if (!await CanUserManageCompanyAsync(companyId, userId))
                {
                    return new List<CompanyRecruiterListDto>();
                }

                var company = await _context.Companies
                    .Include(c => c.CompanyRecruiters)
                        .ThenInclude(cr => cr.User)
                            .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return new List<CompanyRecruiterListDto>();
                }

                var result = new List<CompanyRecruiterListDto>();

                foreach (var recruiter in company.CompanyRecruiters.Where(cr => cr.IsApproved == true))
                {
                    var positionCount = company.Positions.Count(p => p.CreatedBy == recruiter.UserId);

                    result.Add(new CompanyRecruiterListDto
                    {
                        CompanyId = companyId,
                        CompanyName = company.Name,
                        CompanyLogoUrl = company.LogoUrl,
                        UserId = recruiter.UserId,
                        UserName = recruiter.User.UserProfile != null
                            ? $"{recruiter.User.UserProfile.FirstName} {recruiter.User.UserProfile.LastName}".Trim()
                            : recruiter.User.Email,
                        UserEmail = recruiter.User.Email,
                        UserProfilePicture = recruiter.User.UserProfile?.ProfilePictureUrl,
                        JoinedAt = recruiter.JoinedAt,
                        IsApproved = recruiter.IsApproved ?? false,
                        Status = recruiter.UserId == company.CreatedBy ? "Owner" : "Active",
                        RequestMessage = recruiter.RequestMessage,
                        ResponseMessage = recruiter.ResponseMessage,
                        PositionCount = positionCount,
                        LastActivity = recruiter.LastActivity
                    });
                }

                return result.OrderByDescending(r => r.Status == "Owner").ThenBy(r => r.UserName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company recruiters for company {CompanyId}", companyId);
                return new List<CompanyRecruiterListDto>();
            }
        }

        public async Task<List<CompanyRecruiterListDto>> GetPendingRecruiterRequestsAsync(int companyId, int userId)
        {
            try
            {
                // Check if user can view this company's recruiters
                if (!await CanUserManageCompanyAsync(companyId, userId))
                {
                    return new List<CompanyRecruiterListDto>();
                }

                // Get company info first
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
                if (company == null)
                {
                    return new List<CompanyRecruiterListDto>();
                }

                // Get pending recruiters with explicit joins, excluding company owner and rejected requests
                var pendingRecruiters = await (from cr in _context.CompanyRecruiters
                                             join u in _context.Users on cr.UserId equals u.UserId
                                             join up in _context.UserProfiles on u.UserId equals up.UserId into upGroup
                                             from up in upGroup.DefaultIfEmpty()
                                             join inviter in _context.Users on cr.InvitedBy equals inviter.UserId into inviterGroup
                                             from inviter in inviterGroup.DefaultIfEmpty()
                                             join inviterProfile in _context.UserProfiles on inviter.UserId equals inviterProfile.UserId into inviterProfileGroup
                                             from inviterProfile in inviterProfileGroup.DefaultIfEmpty()
                                             where cr.CompanyId == companyId
                                                && cr.IsApproved == false
                                                && (cr.Status == "pending" || cr.Status == "invited")
                                                && cr.UserId != company.CreatedBy  // Exclude company owner
                                             select new
                                             {
                                                 CompanyRecruiter = cr,
                                                 User = u,
                                                 UserProfile = up,
                                                 Inviter = inviter,
                                                 InviterProfile = inviterProfile
                                             }).ToListAsync();

                var result = new List<CompanyRecruiterListDto>();

                foreach (var item in pendingRecruiters)
                {
                    var recruiter = item.CompanyRecruiter;
                    var user = item.User;
                    var userProfile = item.UserProfile;
                    var inviter = item.Inviter;
                    var inviterProfile = item.InviterProfile;

                    result.Add(new CompanyRecruiterListDto
                    {
                        CompanyId = companyId,
                        CompanyName = company.Name,
                        CompanyLogoUrl = company.LogoUrl,
                        UserId = recruiter.UserId,
                        UserName = userProfile != null
                            ? $"{userProfile.FirstName} {userProfile.LastName}".Trim()
                            : user.Email,
                        UserEmail = user.Email,
                        UserProfilePicture = userProfile?.ProfilePictureUrl,
                        JoinedAt = null,
                        IsApproved = false,
                        Status = recruiter.Status ?? "Pending",
                        RequestMessage = recruiter.RequestMessage,
                        ResponseMessage = recruiter.ResponseMessage,
                        PositionCount = 0,
                        LastActivity = recruiter.LastActivity,
                        InvitedBy = recruiter.InvitedBy,
                        InvitedByName = inviter != null
                            ? (inviterProfile != null
                                ? $"{inviterProfile.FirstName} {inviterProfile.LastName}".Trim()
                                : inviter.Email)
                            : null
                    });
                }

                return result.OrderByDescending(r => r.LastActivity).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending recruiter requests for company {CompanyId}", companyId);
                return new List<CompanyRecruiterListDto>();
            }
        }

        public async Task<List<CompanyRecruiterListDto>> GetUserCompanyRequestsAsync(int userId)
        {
            try
            {
                var requests = await _context.CompanyRecruiters
                    .Include(cr => cr.Company)
                    .Where(cr => cr.UserId == userId && cr.IsApproved == false)
                    .ToListAsync();

                var result = new List<CompanyRecruiterListDto>();

                foreach (var request in requests)
                {
                    result.Add(new CompanyRecruiterListDto
                    {
                        CompanyId = request.CompanyId,
                        CompanyName = request.Company.Name,
                        CompanyLogoUrl = request.Company.LogoUrl,
                        UserId = userId,
                        UserName = "", // Not needed for user's own requests
                        UserEmail = "", // Not needed for user's own requests
                        JoinedAt = null,
                        IsApproved = false,
                        Status = request.Status ?? "Pending",
                        RequestMessage = request.RequestMessage,
                        ResponseMessage = request.ResponseMessage,
                        PositionCount = 0,
                        LastActivity = request.LastActivity
                    });
                }

                return result.OrderByDescending(r => r.LastActivity).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user company requests for user {UserId}", userId);
                return new List<CompanyRecruiterListDto>();
            }
        }

        public async Task<CompanyRecruiterStatisticsDto> GetCompanyRecruiterStatisticsAsync(int companyId, int userId)
        {
            try
            {
                // Check if user can view this company's statistics
                if (!await CanUserManageCompanyAsync(companyId, userId))
                {
                    return new CompanyRecruiterStatisticsDto();
                }

                var company = await _context.Companies
                    .Include(c => c.CompanyRecruiters)
                        .ThenInclude(cr => cr.User)
                            .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return new CompanyRecruiterStatisticsDto();
                }

                var activeRecruiters = company.CompanyRecruiters.Where(cr => cr.IsApproved == true).ToList();
                var pendingRequests = company.CompanyRecruiters.Where(cr => cr.IsApproved == false).ToList();
                var totalPositions = company.Positions.Count;
                var totalApplications = company.Positions.Sum(p => p.Applications.Count);

                var topPerformers = new List<RecruiterPerformanceDto>();

                foreach (var recruiter in activeRecruiters)
                {
                    var recruiterPositions = company.Positions.Where(p => p.CreatedBy == recruiter.UserId).ToList();
                    var recruiterApplications = recruiterPositions.Sum(p => p.Applications.Count);
                    var successRate = recruiterPositions.Count > 0
                        ? (double)recruiterApplications / recruiterPositions.Count
                        : 0;

                    topPerformers.Add(new RecruiterPerformanceDto
                    {
                        UserId = recruiter.UserId,
                        Name = recruiter.User.UserProfile != null
                            ? $"{recruiter.User.UserProfile.FirstName} {recruiter.User.UserProfile.LastName}".Trim()
                            : recruiter.User.Email,
                        Email = recruiter.User.Email,
                        PositionCount = recruiterPositions.Count,
                        ApplicationCount = recruiterApplications,
                        SuccessRate = successRate,
                        LastActivity = recruiter.LastActivity
                    });
                }

                return new CompanyRecruiterStatisticsDto
                {
                    TotalRecruiters = activeRecruiters.Count,
                    ActiveRecruiters = activeRecruiters.Count(r => r.LastActivity > DateTimeOffset.UtcNow.AddDays(-30)),
                    PendingRequests = pendingRequests.Count,
                    TotalPositions = totalPositions,
                    TotalApplications = totalApplications,
                    TopPerformers = topPerformers.OrderByDescending(p => p.ApplicationCount).Take(5).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company recruiter statistics for company {CompanyId}", companyId);
                return new CompanyRecruiterStatisticsDto();
            }
        }

        public async Task<CompanyStatisticsDto> GetCompanyStatisticsAsync(int companyId, int userId)
        {
            try
            {
                // Check if user can view this company's statistics
                if (!await CanUserManageCompanyAsync(companyId, userId))
                {
                    return new CompanyStatisticsDto();
                }

                var company = await _context.Companies
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.CompanyRecruiters)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null)
                {
                    return new CompanyStatisticsDto();
                }

                var positions = company.Positions.ToList();
                var applications = positions.SelectMany(p => p.Applications).ToList();
                var recruiters = company.CompanyRecruiters.Where(cr => cr.IsApproved == true).ToList();

                var totalPositions = positions.Count;
                var activePositions = positions.Count(p => p.IsActive == true);
                var totalApplications = applications.Count;
                var pendingApplications = applications.Count(a => a.CurrentStatus == "applied");
                var acceptedApplications = applications.Count(a => a.CurrentStatus == "accepted");
                var rejectedApplications = applications.Count(a => a.CurrentStatus == "rejected");

                var avgApplicationsPerPosition = totalPositions > 0 ? (double)totalApplications / totalPositions : 0;
                var successRate = totalApplications > 0 ? (double)acceptedApplications / totalApplications * 100 : 0;

                // Monthly statistics
                var monthlyStats = new List<MonthlyCompanyStatDto>();
                var last12Months = Enumerable.Range(0, 12)
                    .Select(i => DateTimeOffset.UtcNow.AddMonths(-i))
                    .OrderBy(d => d)
                    .ToList();

                foreach (var month in last12Months)
                {
                    var monthPositions = positions.Where(p => p.CreatedAt?.Year == month.Year && p.CreatedAt?.Month == month.Month).ToList();
                    var monthApplications = applications.Where(a => a.AppliedAt?.Year == month.Year && a.AppliedAt?.Month == month.Month).ToList();
                    var monthHired = monthApplications.Count(a => a.CurrentStatus == "accepted");

                    monthlyStats.Add(new MonthlyCompanyStatDto
                    {
                        Year = month.Year,
                        Month = month.Month,
                        MonthName = month.ToString("MMM yyyy"),
                        PositionCount = monthPositions.Count,
                        ApplicationCount = monthApplications.Count,
                        HiredCount = monthHired
                    });
                }

                // Category statistics
                var categoryStats = positions
                    .Where(p => p.Category != null)
                    .GroupBy(p => p.Category.CategoryName)
                    .Select(g => new CategoryCompanyStatDto
                    {
                        CategoryName = g.Key,
                        PositionCount = g.Count(),
                        ApplicationCount = g.SelectMany(p => p.Applications).Count(),
                        SuccessRate = g.SelectMany(p => p.Applications).Count() > 0
                            ? (double)g.SelectMany(p => p.Applications).Count(a => a.CurrentStatus == "accepted") / g.SelectMany(p => p.Applications).Count() * 100
                            : 0
                    })
                    .OrderByDescending(c => c.PositionCount)
                    .ToList();

                return new CompanyStatisticsDto
                {
                    TotalPositions = totalPositions,
                    ActivePositions = activePositions,
                    TotalApplications = totalApplications,
                    PendingApplications = pendingApplications,
                    AcceptedApplications = acceptedApplications,
                    RejectedApplications = rejectedApplications,
                    TotalRecruiters = recruiters.Count,
                    ActiveRecruiters = recruiters.Count(r => r.LastActivity > DateTimeOffset.UtcNow.AddDays(-30)),
                    AverageApplicationsPerPosition = avgApplicationsPerPosition,
                    SuccessRate = successRate,
                    MonthlyStats = monthlyStats,
                    CategoryStats = categoryStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company statistics for company {CompanyId}", companyId);
                return new CompanyStatisticsDto();
            }
        }

        public async Task<List<CompanyListDto>> GetTopCompaniesAsync(int count = 10)
        {
            try
            {
                var companies = await _context.Companies
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.CompanyRecruiters)
                    .ToListAsync();

                return companies
                    .Select(c => new CompanyListDto
                    {
                        CompanyId = c.CompanyId,
                        Name = c.Name,
                        Description = c.Description,
                        LogoUrl = c.LogoUrl,
                        Website = c.Website,
                        Industry = c.Industry,
                        Location = c.Location,
                        CreatedAt = c.CreatedAt,
                        CreatedBy = c.CreatedBy,
                        CreatedByName = c.CreatedByNavigation?.UserProfile != null
                            ? $"{c.CreatedByNavigation.UserProfile.FirstName} {c.CreatedByNavigation.UserProfile.LastName}".Trim()
                            : c.CreatedByNavigation?.Email,
                        PositionCount = c.Positions.Count,
                        ActivePositionCount = c.Positions.Count(p => p.IsActive == true),
                        TotalApplications = c.Positions.SelectMany(p => p.Applications).Count(),
                        RecruiterCount = c.CompanyRecruiters.Count(cr => cr.IsApproved == true)
                    })
                    .OrderByDescending(c => c.TotalApplications)
                    .ThenByDescending(c => c.ActivePositionCount)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top companies");
                return new List<CompanyListDto>();
            }
        }

        public async Task<List<CompanyListDto>> GetRecentCompaniesAsync(int count = 10)
        {
            try
            {
                var companies = await _context.Companies
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.Positions)
                        .ThenInclude(p => p.Applications)
                    .Include(c => c.CompanyRecruiters)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return companies
                    .Select(c => new CompanyListDto
                    {
                        CompanyId = c.CompanyId,
                        Name = c.Name,
                        Description = c.Description,
                        LogoUrl = c.LogoUrl,
                        Website = c.Website,
                        Industry = c.Industry,
                        Location = c.Location,
                        CreatedAt = c.CreatedAt,
                        CreatedBy = c.CreatedBy,
                        CreatedByName = c.CreatedByNavigation?.UserProfile != null
                            ? $"{c.CreatedByNavigation.UserProfile.FirstName} {c.CreatedByNavigation.UserProfile.LastName}".Trim()
                            : c.CreatedByNavigation?.Email,
                        PositionCount = c.Positions.Count,
                        ActivePositionCount = c.Positions.Count(p => p.IsActive == true),
                        TotalApplications = c.Positions.SelectMany(p => p.Applications).Count(),
                        RecruiterCount = c.CompanyRecruiters.Count(cr => cr.IsApproved == true)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent companies");
                return new List<CompanyListDto>();
            }
        }

        // Notification Methods
        private async Task SendNewCompanyNotificationsAsync(int companyId, string companyName, int createdBy)
        {
            try
            {
                _logger.LogInformation("Sending new company notifications for company: {CompanyId}", companyId);

                // Get all admin users to notify them about new company registration
                var adminUsers = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "Admin" && u.IsActive == true)
                    .Select(u => u.UserId)
                    .ToListAsync();

                foreach (var adminId in adminUsers)
                {
                    await _notificationIntegration.NotifyCompanyRegistrationAsync(
                        adminId,
                        companyName,
                        companyId,
                        "Cần xem xét và phê duyệt"
                    );
                }

                _logger.LogInformation("Sent new company notifications to {Count} admins for company {CompanyId}",
                    adminUsers.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new company notifications for company: {CompanyId}", companyId);
            }
        }

        private async Task SendCompanyUpdateNotificationsAsync(int companyId, string companyName, int updatedBy)
        {
            try
            {
                _logger.LogInformation("Sending company update notifications for company: {CompanyId}", companyId);

                // Get all company recruiters to notify them about profile update
                var companyRecruiters = await _context.CompanyRecruiters
                    .Where(cr => cr.CompanyId == companyId && cr.IsApproved == true && cr.UserId != updatedBy)
                    .Select(cr => cr.UserId)
                    .ToListAsync();

                foreach (var recruiterId in companyRecruiters)
                {
                    await _notificationIntegration.NotifyCompanyProfileUpdatedAsync(
                        recruiterId,
                        companyName,
                        companyId
                    );
                }

                _logger.LogInformation("Sent company update notifications to {Count} recruiters for company {CompanyId}",
                    companyRecruiters.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending company update notifications for company: {CompanyId}", companyId);
            }
        }

        private async Task SendRecruiterRequestResponseNotificationsAsync(int companyId, int userId, bool isApproved, string requestType)
        {
            try
            {
                _logger.LogInformation("Sending recruiter request response notifications for company: {CompanyId}, user: {UserId}", companyId, userId);

                var company = await _context.Companies.FindAsync(companyId);
                if (company == null) return;

                if (isApproved)
                {
                    // Notify user about approval
                    await _notificationIntegration.NotifyCompanyInvitationAsync(
                        userId,
                        company.Name,
                        companyId,
                        "Yêu cầu tham gia đã được phê duyệt"
                    );

                    // Notify other company recruiters about new member
                    var otherRecruiters = await _context.CompanyRecruiters
                        .Where(cr => cr.CompanyId == companyId && cr.IsApproved == true && cr.UserId != userId)
                        .Select(cr => cr.UserId)
                        .ToListAsync();

                    var newMemberName = await GetUserDisplayNameAsync(userId);
                    foreach (var recruiterId in otherRecruiters)
                    {
                        await _notificationIntegration.NotifyNewCompanyFollowerAsync(
                            recruiterId,
                            newMemberName,
                            companyId
                        );
                    }
                }
                else
                {
                    // Notify user about rejection
                    await _notificationIntegration.NotifyCompanyInvitationAsync(
                        userId,
                        company.Name,
                        companyId,
                        "Yêu cầu tham gia đã bị từ chối"
                    );
                }

                _logger.LogInformation("Sent recruiter request response notifications for company {CompanyId}", companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending recruiter request response notifications for company: {CompanyId}", companyId);
            }
        }

        private async Task<string> GetUserDisplayNameAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user?.UserProfile != null)
                {
                    return $"{user.UserProfile.FirstName} {user.UserProfile.LastName}".Trim();
                }

                return user?.Email ?? "Unknown User";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user display name for user: {UserId}", userId);
                return "Unknown User";
            }
        }
    }
}
