namespace DKyThucTap.Models.DTOs.Application
{
    public class ApplicationCreateDto
    {
        public int PositionId { get; set; }
        public string? CoverLetter { get; set; }
        public string? AdditionalInfo { get; set; } // JSON (ví dụ: câu hỏi thêm)
    }
}
