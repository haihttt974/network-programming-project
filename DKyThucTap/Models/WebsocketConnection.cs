using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class WebsocketConnection
{
    public string ConnectionId { get; set; } = null!;

    public int UserId { get; set; }

    public DateTimeOffset? ConnectedAt { get; set; }

    public DateTimeOffset? LastActivity { get; set; }

    public string? ClientInfo { get; set; }

    public virtual User User { get; set; } = null!;
}
