namespace DKyThucTap.ViewModels
{
    public class CandidateProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CvUrl { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public List<string> Skills { get; set; } = new();
    }
}
