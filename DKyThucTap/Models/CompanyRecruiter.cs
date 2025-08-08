using System;
using System.Collections.Generic;

namespace DKyThucTap.Models
{
    public partial class CompanyRecruiter
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public string? RoleInCompany { get; set; }
        public bool? IsAdmin { get; set; }
        public DateTimeOffset? AssignedAt { get; set; }

        // Enhanced properties for invitation/approval system
        public bool? IsApproved { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }
        public string? RequestMessage { get; set; }
        public string? ResponseMessage { get; set; }
        public int? InvitedBy { get; set; }
        public int? RespondedBy { get; set; }
        public DateTimeOffset? RespondedAt { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? LastActivity { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Company Company { get; set; } = null!;
        public virtual User? InvitedByNavigation { get; set; }
        public virtual User? RespondedByNavigation { get; set; }
    }
}
