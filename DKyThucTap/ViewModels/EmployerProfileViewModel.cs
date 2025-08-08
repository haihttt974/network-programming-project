namespace DKyThucTap.ViewModels
{
    public class EmployerProfileViewModel
    {
        public string CompanyName { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public string? LogoUrl { get; set; }   // ✅ Thêm dòng này
        public double AverageRating { get; set; }
        public List<string> ActivePositions { get; set; } = new();
        public List<string> Reviews { get; set; } = new();
    }
}