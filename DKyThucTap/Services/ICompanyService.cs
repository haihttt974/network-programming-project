using DKyThucTap.Models;
using DKyThucTap.Models.DTOs.Company;

namespace DKyThucTap.Services
{
    public interface ICompanyService
    {
        // CRUD Operations
        Task<(bool Success, string Message, CompanyDetailDto? Company)> CreateCompanyAsync(CreateCompanyDto createDto, int createdBy);
        Task<(bool Success, string Message, CompanyDetailDto? Company)> UpdateCompanyAsync(int companyId, UpdateCompanyDto updateDto, int userId);
        Task<(bool Success, string Message)> DeleteCompanyAsync(int companyId, int userId);
        
        // Read Operations
        Task<CompanyDetailDto?> GetCompanyByIdAsync(int companyId, int? userId = null);
        Task<List<CompanyListDto>> GetAllCompaniesAsync(int? userId = null);
        Task<List<CompanyListDto>> GetUserCompaniesAsync(int userId);
        Task<List<CompanyListDto>> SearchCompaniesAsync(string searchTerm, int? userId = null);
        
        // Company-Recruiter Relationship Management
        Task<(bool Success, string Message)> RequestToJoinCompanyAsync(int companyId, int userId, string? message = null);
        Task<(bool Success, string Message)> InviteRecruiterAsync(int companyId, string userEmail, int invitedBy, string? message = null);
        Task<(bool Success, string Message)> RespondToRecruiterRequestAsync(int companyId, int userId, bool isApproved, int respondedBy, string? responseMessage = null);
        Task<(bool Success, string Message)> RemoveRecruiterAsync(int companyId, int userId, int removedBy);
        Task<(bool Success, string Message)> LeaveCompanyAsync(int companyId, int userId);
        
        // Company-Recruiter Queries
        Task<List<CompanyRecruiterListDto>> GetCompanyRecruitersAsync(int companyId, int userId);
        Task<List<CompanyRecruiterListDto>> GetPendingRecruiterRequestsAsync(int companyId, int userId);
        Task<List<CompanyRecruiterListDto>> GetUserCompanyRequestsAsync(int userId);
        Task<CompanyRecruiterStatisticsDto> GetCompanyRecruiterStatisticsAsync(int companyId, int userId);
        
        // Authorization & Validation
        Task<bool> CanUserManageCompanyAsync(int companyId, int userId);
        Task<bool> IsUserCompanyRecruiterAsync(int companyId, int userId);
        Task<bool> IsUserCompanyOwnerAsync(int companyId, int userId);
        Task<string> GetUserRoleInCompanyAsync(int companyId, int userId);
        
        // Statistics & Analytics
        Task<CompanyStatisticsDto> GetCompanyStatisticsAsync(int companyId, int userId);
        Task<List<CompanyListDto>> GetTopCompaniesAsync(int count = 10);
        Task<List<CompanyListDto>> GetRecentCompaniesAsync(int count = 10);
    }

    public class CompanyStatisticsDto
    {
        public int TotalPositions { get; set; }
        public int ActivePositions { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int AcceptedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public int TotalRecruiters { get; set; }
        public int ActiveRecruiters { get; set; }
        public double AverageApplicationsPerPosition { get; set; }
        public double SuccessRate { get; set; }
        public List<MonthlyCompanyStatDto> MonthlyStats { get; set; } = new List<MonthlyCompanyStatDto>();
        public List<CategoryCompanyStatDto> CategoryStats { get; set; } = new List<CategoryCompanyStatDto>();
    }

    public class MonthlyCompanyStatDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = null!;
        public int PositionCount { get; set; }
        public int ApplicationCount { get; set; }
        public int HiredCount { get; set; }
    }

    public class CategoryCompanyStatDto
    {
        public string CategoryName { get; set; } = null!;
        public int PositionCount { get; set; }
        public int ApplicationCount { get; set; }
        public double SuccessRate { get; set; }
    }
}
