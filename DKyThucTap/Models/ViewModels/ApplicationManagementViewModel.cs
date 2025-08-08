using DKyThucTap.Models.DTOs.Application;
using DKyThucTap.Models.DTOs.Position;

namespace DKyThucTap.Models.ViewModels
{
    public class ApplicationManagementViewModel
    {
        public PositionDetailDto Position { get; set; } = new PositionDetailDto();
        public List<ApplicationListDto> Applications { get; set; } = new List<ApplicationListDto>();
        public ApplicationStatisticsDto Statistics { get; set; } = new ApplicationStatisticsDto();
        public ApplicationSearchDto SearchCriteria { get; set; } = new ApplicationSearchDto();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        
        // Permission flags
        public bool CanManageApplications { get; set; }
        public bool CanViewApplicantDetails { get; set; }
        public bool CanAddNotes { get; set; }
        
        // Available status options
        public List<string> AvailableStatuses { get; set; } = new List<string>
        {
            "applied", "reviewing", "interviewed", "accepted", "rejected"
        };
    }

    public class ApplicationDetailViewModel
    {
        public ApplicationDetailDto Application { get; set; } = new ApplicationDetailDto();
        public PositionDetailDto Position { get; set; } = new PositionDetailDto();
        public bool CanManageApplication { get; set; }
        public bool CanAddNotes { get; set; }
        public List<string> AvailableStatuses { get; set; } = new List<string>
        {
            "applied", "reviewing", "interviewed", "accepted", "rejected"
        };
    }

    public class BulkApplicationActionViewModel
    {
        public List<int> SelectedApplicationIds { get; set; } = new List<int>();
        public string Action { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int PositionId { get; set; }
    }

    public class ApplicationDashboardViewModel
    {
        public ApplicationStatisticsDto OverallStatistics { get; set; } = new ApplicationStatisticsDto();
        public List<ApplicationListDto> RecentApplications { get; set; } = new List<ApplicationListDto>();
    }

    public class PositionApplicationSummaryDto
    {
        public int PositionId { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int AcceptedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public DateTimeOffset? LastApplicationDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ApplicationTrendDto
    {
        public DateTime Date { get; set; }
        public int ApplicationCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class ApplicationNotificationViewModel
    {
        public int ApplicationId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTimeOffset AppliedAt { get; set; }
        public bool HasCoverLetter { get; set; }
        public bool HasCv { get; set; }
    }
}
