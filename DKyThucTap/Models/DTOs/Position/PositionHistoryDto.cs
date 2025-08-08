namespace DKyThucTap.Models.DTOs.Position
{
    public class PositionHistoryDto
    {
        public int HistoryId { get; set; }
        public int PositionId { get; set; }
        public int? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public DateTimeOffset? ChangedAt { get; set; }
        public string ChangeType { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Notes { get; set; }
    }

    public class PositionChangeTracker
    {
        public string FieldName { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool HasChanged => OldValue != NewValue;
    }
}
