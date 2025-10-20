using ProjectScheduler.Services;

namespace ProjectScheduler.Models;

public class AlgorithmComparison
{
    public string AlgorithmType { get; set; } = null!;
    public string AlgorithmName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool CanAllocate { get; set; }
    public string? Message { get; set; }
    public DateTime? SuggestedStartDate { get; set; }
    public DateTime? EstimatedCodeCompleteDate { get; set; }
    public DateTime? EstimatedCrpDate { get; set; }
    public DateTime? EstimatedUatDate { get; set; }
    public DateTime? EstimatedGoLiveDate { get; set; }
    public int? EstimatedDurationDays { get; set; }
    public decimal? BufferedDevHours { get; set; }
    public string? Pros { get; set; }
    public string? Cons { get; set; }
}
