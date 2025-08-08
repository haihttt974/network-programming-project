using System.ComponentModel.DataAnnotations;

namespace DKyThucTap.Models.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d@$!%*?&]{6,}$",
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm 1 chữ hoa, 1 chữ thường và 1 số")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ không được vượt quá 100 ký tự")]
        public string LastName { get; set; } = null!;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }
    }
}
