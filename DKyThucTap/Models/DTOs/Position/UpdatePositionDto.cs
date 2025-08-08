using System.ComponentModel.DataAnnotations;

namespace DKyThucTap.Models.DTOs.Position
{
    public class UpdatePositionDto
    {
        [Required(ErrorMessage = "Tiêu đề vị trí là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Mô tả công việc là bắt buộc")]
        [StringLength(5000, ErrorMessage = "Mô tả không được vượt quá 5000 ký tự")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Loại công việc là bắt buộc")]
        [StringLength(50, ErrorMessage = "Loại công việc không được vượt quá 50 ký tự")]
        public string PositionType { get; set; } = null!;

        [StringLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự")]
        public string? Location { get; set; }

        public bool IsRemote { get; set; } = false;

        [StringLength(100, ErrorMessage = "Mức lương không được vượt quá 100 ký tự")]
        public string? SalaryRange { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? ApplicationDeadline { get; set; }

        public int? CategoryId { get; set; }

        public List<int> SkillIds { get; set; } = new List<int>();

        public bool IsActive { get; set; } = true;
    }
}
