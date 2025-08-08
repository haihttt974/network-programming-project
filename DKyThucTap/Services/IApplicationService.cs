using DKyThucTap.Models.DTOs.Application;
using DKyThucTap.Models.ViewModels;

namespace DKyThucTap.Services
{
    public interface IApplicationService
    {
        // CRUD Operations
        Task<ApplicationDetailDto?> GetApplicationByIdAsync(int applicationId, int userId);
        Task<ApplicationSearchResultDto> GetApplicationsAsync(ApplicationSearchDto searchDto, int userId);
        Task<List<ApplicationListDto>> GetApplicationsByPositionAsync(int positionId, int userId);
        Task<List<ApplicationListDto>> GetApplicationsByUserAsync(int userId);
        Task<ApplicationStatisticsDto> GetApplicationStatisticsAsync(int? positionId, int? companyId, int userId);

        // Status Management
        Task<(bool Success, string Message)> UpdateApplicationStatusAsync(int applicationId, string newStatus, string? notes, int changedBy);
        Task<(bool Success, string Message, int UpdatedCount)> BulkUpdateApplicationStatusAsync(List<int> applicationIds, string newStatus, string? notes, int changedBy);
        Task<List<ApplicationStatusHistoryDto>> GetApplicationStatusHistoryAsync(int applicationId, int userId);

        // Notes Management
        Task<(bool Success, string Message)> AddApplicantNoteAsync(int applicationId, string noteText, int interviewerUserId);
        Task<List<ApplicantNoteDto>> GetApplicantNotesAsync(int applicationId, int userId);
        Task<(bool Success, string Message)> DeleteApplicantNoteAsync(int noteId, int userId);

        // Permissions
        Task<bool> CanUserManageApplicationAsync(int applicationId, int userId);
        Task<bool> CanUserViewApplicationAsync(int applicationId, int userId);
        Task<bool> CanUserManagePositionApplicationsAsync(int positionId, int userId);

        // Dashboard and Analytics
        Task<List<ApplicationListDto>> GetRecentApplicationsAsync(int userId, int count = 10);

        // Validation
        Task<bool> IsValidStatusTransitionAsync(string currentStatus, string newStatus);
        Task<List<string>> GetAvailableStatusesAsync(string currentStatus);
        Task<bool> ApplicationExistsAsync(int applicationId);

        // Bulk Operations
        Task<(bool Success, string Message)> BulkDeleteApplicationsAsync(List<int> applicationIds, int userId);
        Task<List<ApplicationListDto>> GetApplicationsForBulkActionAsync(List<int> applicationIds, int userId);
    }
}
