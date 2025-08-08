using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class Company
{
    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string? Website { get; set; }

    public string? Industry { get; set; }

    public string? Location { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual ICollection<CompanyReview> CompanyReviews { get; set; } = new List<CompanyReview>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual ICollection<CompanyRecruiter> CompanyRecruiters { get; set; } = new List<CompanyRecruiter>();
}
