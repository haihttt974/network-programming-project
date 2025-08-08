using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class Position
{
    public int PositionId { get; set; }

    public int CompanyId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PositionType { get; set; } = null!;

    public string? Location { get; set; }

    public bool? IsRemote { get; set; }

    public string? SalaryRange { get; set; }

    public DateOnly? ApplicationDeadline { get; set; }

    public bool? IsActive { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? CategoryId { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual JobCategory? Category { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<PositionHistory> PositionHistories { get; set; } = new List<PositionHistory>();

    public virtual ICollection<PositionSkill> PositionSkills { get; set; } = new List<PositionSkill>();
}
