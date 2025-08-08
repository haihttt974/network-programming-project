namespace DKyThucTap.Models.DTOs.Position
{
    public class PositionListDto
    {
        public int PositionId { get; set; }
        public string Title { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string? CompanyLogoUrl { get; set; }
        public string PositionType { get; set; } = null!;
        public string? Location { get; set; }
        public bool? IsRemote { get; set; }
        public string? SalaryRange { get; set; }
        public DateOnly? ApplicationDeadline { get; set; }
        public bool? IsActive { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public string? CategoryName { get; set; }
        public int ApplicationCount { get; set; }
        public List<string> RequiredSkills { get; set; } = new List<string>();
    }
}
