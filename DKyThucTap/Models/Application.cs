using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class Application
{
    public int ApplicationId { get; set; }

    public int PositionId { get; set; }

    public int UserId { get; set; }

    public string CurrentStatus { get; set; } = null!;

    public DateTimeOffset? AppliedAt { get; set; }

    public string? CoverLetter { get; set; }

    public string? AdditionalInfo { get; set; }

    public virtual ICollection<ApplicantNote> ApplicantNotes { get; set; } = new List<ApplicantNote>();

    public virtual ICollection<ApplicationStatusHistory> ApplicationStatusHistories { get; set; } = new List<ApplicationStatusHistory>();

    public virtual Position Position { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
