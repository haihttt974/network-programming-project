using System.ComponentModel.DataAnnotations;

namespace DKyThucTap.Models.DTOs.Company
{
    public class UpdateCompanyDto
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên công ty không được vượt quá 255 ký tự")]
        public string Name { get; set; } = null!;

        [StringLength(5000, ErrorMessage = "Mô tả không được vượt quá 5000 ký tự")]
        public string? Description { get; set; }

        [StringLength(255, ErrorMessage = "URL logo không được vượt quá 255 ký tự")]
        [Url(ErrorMessage = "URL logo không hợp lệ")]
        public string? LogoUrl { get; set; }

        [StringLength(255, ErrorMessage = "Website không được vượt quá 255 ký tự")]
        [Url(ErrorMessage = "URL website không hợp lệ")]
        public string? Website { get; set; }

        [StringLength(100, ErrorMessage = "Ngành nghề không được vượt quá 100 ký tự")]
        public string? Industry { get; set; }

        [StringLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự")]
        public string? Location { get; set; }
    }
}
