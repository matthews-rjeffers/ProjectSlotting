namespace ProjectScheduler.Models;

public class AllocationConflict
{
    public string ConflictType { get; set; } = null!; // "CapacityOverload", "HighUtilization", "OnsiteOverlap"
    public string Severity { get; set; } = null!; // "Critical", "Warning"
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public DateTime? ConflictDate { get; set; }
    public DateTime? ConflictWeekStart { get; set; }
    public decimal? CurrentUtilization { get; set; }
    public decimal? ProjectedUtilization { get; set; }
    public List<string>? ConflictingProjects { get; set; }
}

public class ConflictCheckResult
{
    public bool HasConflicts { get; set; }
    public bool RequiresConfirmation { get; set; }
    public List<AllocationConflict> Conflicts { get; set; } = new();
    public string? SummaryMessage { get; set; }
}
