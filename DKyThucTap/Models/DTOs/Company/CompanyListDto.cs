namespace DKyThucTap.Models.DTOs.Company
{
    public class CompanyListDto
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
        
        // User relationship
        public string? UserRole { get; set; } // "Owner", "Recruiter", "Pending", null
        public DateTimeOffset? JoinedAt { get; set; }
        public bool IsApproved { get; set; }
    }
}
