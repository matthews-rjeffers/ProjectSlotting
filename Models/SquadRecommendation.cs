using ProjectScheduler.Services;

namespace ProjectScheduler.Models;

public class SquadRecommendation
{
    public int SquadId { get; set; }
    public string SquadName { get; set; } = null!;
    public decimal OverallScore { get; set; } // 0-100
    public decimal CapacityScore { get; set; } // 0-100
    public decimal WorkloadScore { get; set; } // 0-100
    public decimal ProjectCountScore { get; set; } // 0-100
    public decimal SizeScore { get; set; } // 0-100
    public bool CanAllocate { get; set; }
    public string RecommendationReason { get; set; } = null!;
    public ScheduleSuggestion? Suggestion { get; set; }
}
