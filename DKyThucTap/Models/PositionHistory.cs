using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class PositionHistory
{
    public int HistoryId { get; set; }

    public int PositionId { get; set; }

    public int? ChangedByUserId { get; set; }

    public DateTimeOffset? ChangedAt { get; set; }

    public string ChangeType { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Notes { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual Position Position { get; set; } = null!;
}
