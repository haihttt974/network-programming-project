namespace DKyThucTap.ViewModels
{
    public class RecruiterProfileViewModel
    {
        public int RecruiterId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }

        // ✅ Chuyển từ List<string> sang List<RecruiterCompanyDto>
        public List<RecruiterCompanyDto> Companies { get; set; } = new();
        public List<RecruiterPositionDto> PostedPositions { get; set; } = new();
    }

    public class RecruiterCompanyDto
    {
        public int CompanyId { get; set; }
        public string Name { get; set; }
    }

    public class RecruiterPositionDto
    {
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
