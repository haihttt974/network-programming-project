using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class UserProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? CvUrl { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? Bio { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
