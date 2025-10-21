using System;
using System.Collections.Generic;

namespace ProjectScheduler.Models;

public partial class OnsiteSchedule
{
    public int OnsiteScheduleId { get; set; }

    public int ProjectId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int EngineerCount { get; set; }

    public string OnsiteType { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int TotalHours { get; set; }

    public string? Notes { get; set; }

    public virtual Project Project { get; set; } = null!;
}
