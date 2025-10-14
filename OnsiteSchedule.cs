using System;
using System.Collections.Generic;

namespace ProjectScheduler;

public partial class OnsiteSchedule
{
    public int OnsiteScheduleId { get; set; }

    public int ProjectId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public int EngineerCount { get; set; }

    public int TotalHours { get; set; }

    public string OnsiteType { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Project Project { get; set; } = null!;
}
