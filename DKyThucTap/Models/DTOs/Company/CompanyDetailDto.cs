namespace DKyThucTap.Models.DTOs.Company
{
    public class CompanyDetailDto
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        
        // Statistics
        public int PositionCount { get; set; }
        public int ActivePositionCount { get; set; }
        public int TotalApplications { get; set; }
        public int RecruiterCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        
        // User relationship
        public string? UserRole { get; set; } // "Owner", "Recruiter", "Pending", null
        public DateTimeOffset? JoinedAt { get; set; }
        public bool IsApproved { get; set; }
        public bool CanManage { get; set; }
        
        // Recent data
        public List<CompanyPositionDto> RecentPositions { get; set; } = new List<CompanyPositionDto>();
        public List<CompanyRecruiterDto> Recruiters { get; set; } = new List<CompanyRecruiterDto>();
        public List<CompanyReviewDto> RecentReviews { get; set; } = new List<CompanyReviewDto>();
    }

    public class CompanyPositionDto
    {
        public int PositionId { get; set; }
        public string Title { get; set; } = null!;
        public string PositionType { get; set; } = null!;
        public bool? IsActive { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class CompanyRecruiterDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }
        public bool IsApproved { get; set; }
        public string Status { get; set; } = null!; // "Owner", "Active", "Pending"
        public int PositionCount { get; set; }
    }

    public class CompanyReviewDto
    {
        public int ReviewId { get; set; }
        public string ReviewerName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }
}
