using DKyThucTap.Models;
using DKyThucTap.Models.DTOs.Position;

namespace DKyThucTap.Services
{
    public interface IPositionService
    {
        // CRUD Operations
        Task<(bool Success, string Message, PositionDetailDto? Position)> CreatePositionAsync(CreatePositionDto createDto, int createdBy);
        Task<(bool Success, string Message, PositionDetailDto? Position)> UpdatePositionAsync(int positionId, UpdatePositionDto updateDto, int userId);
        Task<(bool Success, string Message)> DeletePositionAsync(int positionId, int userId);
        
        // Read Operations
        Task<PositionDetailDto?> GetPositionByIdAsync(int positionId);
        Task<PositionSearchResultDto> GetPositionsAsync(PositionSearchDto searchDto);
        Task<List<PositionListDto>> GetPositionsByUserAsync(int userId);
        Task<List<PositionListDto>> GetPositionsByCompanyAsync(int companyId);
        Task<List<PositionListDto>> GetActivePositionsAsync();
        
        // Status Management
        Task<(bool Success, string Message)> UpdatePositionStatusAsync(int positionId, bool isActive, int userId);
        Task<(bool Success, string Message)> ExtendDeadlineAsync(int positionId, DateOnly newDeadline, int userId);
        
        // Application Management
        Task<List<PositionApplicationDto>> GetPositionApplicationsAsync(int positionId, int userId);
        Task<int> GetApplicationCountAsync(int positionId);
        
        // Validation & Authorization
        Task<bool> CanUserManagePositionAsync(int positionId, int userId);
        Task<bool> IsPositionActiveAsync(int positionId);
        Task<bool> IsDeadlineValidAsync(DateOnly? deadline);
        
        // Lookup Data
        Task<List<JobCategory>> GetJobCategoriesAsync();
        Task<List<Company>> GetUserCompaniesAsync(int userId);
        Task<List<Skill>> GetSkillsAsync();
        Task<List<Skill>> GetSkillsByCategoryAsync(string category);
        
        // Statistics
        Task<PositionStatisticsDto> GetPositionStatisticsAsync(int userId);
        Task<PositionStatisticsDto> GetCompanyPositionStatisticsAsync(int companyId, int userId);

        // Candidate Matching for Notifications
        Task<List<int>> FindMatchingCandidatesAsync(int positionId);
        Task<List<int>> FindCandidatesBySkillsAsync(List<int> skillIds);
        Task<List<int>> FindCandidatesByLocationAsync(string location);
        Task<List<int>> GetPositionApplicantUserIdsAsync(int positionId);

        // History Tracking
        Task<List<PositionHistoryDto>> GetPositionHistoryAsync(int positionId, int userId);
        Task CreatePositionHistoryAsync(int positionId, string changeType, string? oldValue, string? newValue, int changedByUserId, string? notes = null);
    }

    public class PositionStatisticsDto
    {
        public int TotalPositions { get; set; }
        public int ActivePositions { get; set; }
        public int InactivePositions { get; set; }
        public int ExpiredPositions { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int AcceptedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public double AverageApplicationsPerPosition { get; set; }
        public List<CategoryStatDto> CategoryStats { get; set; } = new List<CategoryStatDto>();
        public List<MonthlyStatDto> MonthlyStats { get; set; } = new List<MonthlyStatDto>();
    }

    public class CategoryStatDto
    {
        public string CategoryName { get; set; } = null!;
        public int PositionCount { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class MonthlyStatDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = null!;
        public int PositionCount { get; set; }
        public int ApplicationCount { get; set; }
    }
}
