using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public int Participant1UserId { get; set; }

    public int Participant2UserId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? LastMessageAt { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User Participant1User { get; set; } = null!;

    public virtual User Participant2User { get; set; } = null!;
}
