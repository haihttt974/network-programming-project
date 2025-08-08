using System.ComponentModel.DataAnnotations;

namespace DKyThucTap.Models.DTOs.Company
{
    public class CompanyRecruiterRequestDto
    {
        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public int CompanyId { get; set; }
        
        [StringLength(500, ErrorMessage = "Lời nhắn không được vượt quá 500 ký tự")]
        public string? Message { get; set; }
    }

    public class CompanyRecruiterInviteDto
    {
        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public int CompanyId { get; set; }
        
        [Required(ErrorMessage = "Email người dùng là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string UserEmail { get; set; } = null!;
        
        [StringLength(500, ErrorMessage = "Lời nhắn không được vượt quá 500 ký tự")]
        public string? Message { get; set; }
    }

    public class CompanyRecruiterResponseDto
    {
        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public int CompanyId { get; set; }
        
        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Quyết định là bắt buộc")]
        public bool IsApproved { get; set; }
        
        [StringLength(500, ErrorMessage = "Lời nhắn không được vượt quá 500 ký tự")]
        public string? ResponseMessage { get; set; }
    }

    public class CompanyRecruiterListDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string? CompanyLogoUrl { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string? UserProfilePicture { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }
        public bool IsApproved { get; set; }
        public string Status { get; set; } = null!; // "Owner", "Active", "Pending", "Invited", "Rejected"
        public string? RequestMessage { get; set; }
        public string? ResponseMessage { get; set; }
        public int PositionCount { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public int? InvitedBy { get; set; } // ID of user who sent the invitation
        public string? InvitedByName { get; set; } // Name of user who sent the invitation
    }

    public class CompanyRecruiterStatisticsDto
    {
        public int TotalRecruiters { get; set; }
        public int ActiveRecruiters { get; set; }
        public int PendingRequests { get; set; }
        public int TotalPositions { get; set; }
        public int TotalApplications { get; set; }
        public List<RecruiterPerformanceDto> TopPerformers { get; set; } = new List<RecruiterPerformanceDto>();
    }

    public class RecruiterPerformanceDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int PositionCount { get; set; }
        public int ApplicationCount { get; set; }
        public double SuccessRate { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
    }
}
