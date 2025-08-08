using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class ApplicationStatusHistory
{
    public int HistoryId { get; set; }

    public int ApplicationId { get; set; }

    public string Status { get; set; } = null!;

    public DateTimeOffset? ChangedAt { get; set; }

    public int? ChangedBy { get; set; }

    public string? Notes { get; set; }

    public virtual Application Application { get; set; } = null!;

    public virtual User? ChangedByNavigation { get; set; }
}
