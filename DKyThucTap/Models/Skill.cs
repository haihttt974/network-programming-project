using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class Skill
{
    public int SkillId { get; set; }

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public virtual ICollection<PositionSkill> PositionSkills { get; set; } = new List<PositionSkill>();

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
