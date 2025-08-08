using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string MessageText { get; set; } = null!;

    public DateTimeOffset? SentAt { get; set; }

    public bool? IsRead { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual User SenderUser { get; set; } = null!;
}
