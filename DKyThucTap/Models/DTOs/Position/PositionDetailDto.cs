namespace DKyThucTap.Models.DTOs.Position
{
    public class PositionDetailDto
    {
        public int PositionId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string PositionType { get; set; } = null!;
        public string? Location { get; set; }
        public bool? IsRemote { get; set; }
        public string? SalaryRange { get; set; }
        public DateOnly? ApplicationDeadline { get; set; }
        public bool? IsActive { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }

        // Company Information
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string? CompanyDescription { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyIndustry { get; set; }
        public string? CompanyLocation { get; set; }

        // Category Information
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryDescription { get; set; }

        // Skills
        public List<PositionSkillDto> RequiredSkills { get; set; } = new List<PositionSkillDto>();

        // Applications
        public int ApplicationCount { get; set; }
        public List<PositionApplicationDto> RecentApplications { get; set; } = new List<PositionApplicationDto>();

        // Statistics
        public bool IsExpired => ApplicationDeadline.HasValue && ApplicationDeadline.Value < DateOnly.FromDateTime(DateTime.Now);
        public int DaysUntilDeadline => ApplicationDeadline.HasValue 
            ? (ApplicationDeadline.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days 
            : int.MaxValue;
    }

    public class PositionSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string? SkillCategory { get; set; }
    }

    public class PositionApplicationDto
    {
        public int ApplicationId { get; set; }
        public int UserId { get; set; }
        public string ApplicantName { get; set; } = null!;
        public string ApplicantEmail { get; set; } = null!;
        public string? ApplicantPhone { get; set; }
        public string CurrentStatus { get; set; } = null!;
        public DateTimeOffset? AppliedAt { get; set; }
        public string? CoverLetter { get; set; }
    }
}
