using System;
using System.Collections.Generic;

namespace ProjectScheduler.Models
{
    public class GanttDataResponse
    {
        public List<GanttSquad> Squads { get; set; } = new();
        public DateRange DateRange { get; set; } = new();
    }

    public class GanttSquad
    {
        public int SquadId { get; set; }
        public string SquadName { get; set; } = null!;
        public List<GanttProject> Projects { get; set; } = new();
    }

    public class GanttProject
    {
        public int ProjectId { get; set; }
        public string ProjectNumber { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DevelopmentPhase? DevelopmentPhase { get; set; }
        public DevelopmentPhase? PolishPhase { get; set; }  // Phase 2: CRP → UAT polish work
        public List<Milestone> Milestones { get; set; } = new();
        public List<OnsitePhase> OnsitePhases { get; set; } = new();
    }

    public class DevelopmentPhase
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class Milestone
    {
        public string Type { get; set; } = null!; // "CRP", "UAT", "GoLive"
        public DateTime Date { get; set; }
    }

    public class OnsitePhase
    {
        public string Type { get; set; } = null!; // "UAT" or "GoLive"
        public DateTime WeekStartDate { get; set; }
        public int EngineerCount { get; set; }
        public int TotalHours { get; set; }
    }

    public class DateRange
    {
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }
}
