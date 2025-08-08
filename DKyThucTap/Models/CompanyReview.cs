using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class CompanyReview
{
    public int ReviewId { get; set; }

    public int UserId { get; set; }

    public int CompanyId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public bool? IsApproved { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
