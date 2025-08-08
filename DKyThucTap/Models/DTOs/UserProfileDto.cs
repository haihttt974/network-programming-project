using System.ComponentModel.DataAnnotations;

namespace DKyThucTap.Models.DTOs
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? CvUrl { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ không được vượt quá 100 ký tự")]
        public string LastName { get; set; } = null!;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        [StringLength(1000, ErrorMessage = "Tiểu sử không được vượt quá 1000 ký tự")]
        public string? Bio { get; set; }

        [Url(ErrorMessage = "URL ảnh đại diện không hợp lệ")]
        [StringLength(255, ErrorMessage = "URL ảnh đại diện không được vượt quá 255 ký tự")]
        public string? ProfilePictureUrl { get; set; }

        [Url(ErrorMessage = "URL CV không hợp lệ")]
        [StringLength(255, ErrorMessage = "URL CV không được vượt quá 255 ký tự")]
        public string? CvUrl { get; set; }
    }
}
