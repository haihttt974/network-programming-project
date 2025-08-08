using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class PositionSkill
{
    public int PositionId { get; set; }

    public int SkillId { get; set; }

    public bool? IsRequired { get; set; }

    public virtual Position Position { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
