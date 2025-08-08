namespace DKyThucTap.Models.DTOs.Application
{
    public class ApplicationDetailDto
    {
        public int ApplicationId { get; set; }
        public int PositionId { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTimeOffset? AppliedAt { get; set; }
        public string? CoverLetter { get; set; }
        public string? AdditionalInfo { get; set; }

        // Applicant Information
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? ApplicantPhone { get; set; }
        public string? ApplicantAddress { get; set; }
        public string? ApplicantBio { get; set; }
        public string? ApplicantProfilePictureUrl { get; set; }
        public string? ApplicantCvUrl { get; set; }

        // Status History
        public List<ApplicationStatusHistoryDto> StatusHistory { get; set; } = new List<ApplicationStatusHistoryDto>();

        // Notes
        public List<ApplicantNoteDto> Notes { get; set; } = new List<ApplicantNoteDto>();

        // Skills
        public List<UserSkillDto> ApplicantSkills { get; set; } = new List<UserSkillDto>();
    }

    public class ApplicationListDto
    {
        public int ApplicationId { get; set; }
        public int PositionId { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? ApplicantPhone { get; set; }
        public string? ApplicantProfilePictureUrl { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTimeOffset? AppliedAt { get; set; }
        public int NotesCount { get; set; }
        public bool HasCoverLetter { get; set; }
        public bool HasCv { get; set; }
    }

    public class ApplicationStatusHistoryDto
    {
        public int HistoryId { get; set; }
        public int ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? ChangedAt { get; set; }
        public int? ChangedBy { get; set; }
        public string? ChangedByName { get; set; }
        public string? Notes { get; set; }
    }

    public class ApplicantNoteDto
    {
        public int NoteId { get; set; }
        public int ApplicationId { get; set; }
        public int InterviewerUserId { get; set; }
        public string InterviewerName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTimeOffset? CreatedAt { get; set; }
    }

    public class UserSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string? SkillCategory { get; set; }
        public int? ProficiencyLevel { get; set; }
    }

    public class UpdateApplicationStatusDto
    {
        public int ApplicationId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class BulkUpdateApplicationStatusDto
    {
        public List<int> ApplicationIds { get; set; } = new List<int>();
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class AddApplicantNoteDto
    {
        public int ApplicationId { get; set; }
        public string NoteText { get; set; } = string.Empty;
    }

    public class ApplicationSearchDto
    {
        public int? PositionId { get; set; }
        public int? CompanyId { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public DateTimeOffset? AppliedFrom { get; set; }
        public DateTimeOffset? AppliedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "AppliedAt";
        public string SortDirection { get; set; } = "desc";
    }

    public class ApplicationSearchResultDto
    {
        public List<ApplicationListDto> Applications { get; set; } = new List<ApplicationListDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class ApplicationStatisticsDto
    {
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ReviewingApplications { get; set; }
        public int AcceptedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public double AcceptanceRate { get; set; }
        public List<ApplicationStatusCountDto> StatusCounts { get; set; } = new List<ApplicationStatusCountDto>();
    }

    public class ApplicationStatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
