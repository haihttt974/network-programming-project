using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class JobCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
}
