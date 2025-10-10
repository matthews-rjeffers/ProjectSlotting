using System;
using System.Collections.Generic;

namespace ProjectScheduler;

public partial class Squad
{
    public int SquadId { get; set; }

    public string SquadName { get; set; } = null!;

    public string SquadLeadName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<ProjectAllocation> ProjectAllocations { get; set; } = new List<ProjectAllocation>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
