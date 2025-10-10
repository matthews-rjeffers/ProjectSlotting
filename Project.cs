using System;
using System.Collections.Generic;

namespace ProjectScheduler;

public partial class Project
{
    public int ProjectId { get; set; }

    public string ProjectNumber { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string CustomerCity { get; set; } = null!;

    public string CustomerState { get; set; } = null!;

    public decimal EstimatedDevHours { get; set; }

    public decimal EstimatedOnsiteHours { get; set; }

    public DateTime? GoLiveDate { get; set; }

    public DateTime Crpdate { get; set; }

    public DateTime? Uatdate { get; set; }

    public string JiraLink { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual ICollection<OnsiteSchedule> OnsiteSchedules { get; set; } = new List<OnsiteSchedule>();

    public virtual ICollection<ProjectAllocation> ProjectAllocations { get; set; } = new List<ProjectAllocation>();
}
