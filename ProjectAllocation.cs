using System;
using System.Collections.Generic;

namespace ProjectScheduler;

public partial class ProjectAllocation
{
    public int AllocationId { get; set; }

    public int ProjectId { get; set; }

    public int SquadId { get; set; }

    public DateOnly AllocationDate { get; set; }

    public decimal AllocatedHours { get; set; }

    public DateTime CreatedDate { get; set; }

    public string AllocationType { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual Squad Squad { get; set; } = null!;
}
