using System;
using System.Collections.Generic;

namespace ProjectScheduler;

public partial class TeamMember
{
    public int TeamMemberId { get; set; }

    public int SquadId { get; set; }

    public string MemberName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public decimal DailyCapacityHours { get; set; }

    public bool IsActive { get; set; }

    public virtual Squad Squad { get; set; } = null!;
}
