using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services;

public class SquadRecommendationService : ISquadRecommendationService
{
    private readonly ProjectSchedulerDbContext _context;
    private readonly ICapacityService _capacityService;
    private readonly IScheduleSuggestionService _scheduleSuggestionService;

    public SquadRecommendationService(
        ProjectSchedulerDbContext context,
        ICapacityService capacityService,
        IScheduleSuggestionService scheduleSuggestionService)
    {
        _context = context;
        _capacityService = capacityService;
        _scheduleSuggestionService = scheduleSuggestionService;
    }

    public async Task<List<SquadRecommendation>> GetSquadRecommendations(
        int projectId,
        decimal? bufferPercentage = null,
        string? algorithmType = null,
        DateTime? startDate = null)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            return new List<SquadRecommendation>();
        }

        var squads = await _context.Squads
            .Where(s => s.IsActive)
            .ToListAsync();
        var recommendations = new List<SquadRecommendation>();

        foreach (var squad in squads)
        {
            var recommendation = await EvaluateSquad(squad, project, bufferPercentage, algorithmType, startDate);
            recommendations.Add(recommendation);
        }

        // Sort by overall score descending
        return recommendations.OrderByDescending(r => r.OverallScore).ToList();
    }

    private async Task<SquadRecommendation> EvaluateSquad(
        Squad squad,
        Project project,
        decimal? bufferPercentage,
        string? algorithmType,
        DateTime? startDate)
    {
        var recommendation = new SquadRecommendation
        {
            SquadId = squad.SquadId,
            SquadName = squad.SquadName
        };

        // Try to get a schedule suggestion for this squad
        var suggestion = await _scheduleSuggestionService.GetScheduleSuggestion(
            project.ProjectId,
            squad.SquadId,
            bufferPercentage,
            algorithmType,
            startDate);

        recommendation.Suggestion = suggestion;
        recommendation.CanAllocate = suggestion.CanAllocate;

        if (!suggestion.CanAllocate)
        {
            // Can't allocate - give low scores
            recommendation.CapacityScore = 0;
            recommendation.WorkloadScore = 0;
            recommendation.ProjectCountScore = 0;
            recommendation.SizeScore = 0;
            recommendation.OverallScore = 0;
            recommendation.RecommendationReason = suggestion.Message;
            return recommendation;
        }

        // Calculate individual scores
        recommendation.CapacityScore = await CalculateCapacityScore(squad, project, suggestion);
        recommendation.WorkloadScore = await CalculateWorkloadScore(squad);
        recommendation.ProjectCountScore = await CalculateProjectCountScore(squad);
        recommendation.SizeScore = await CalculateSizeScore(squad, project);

        // Calculate overall score (weighted average)
        recommendation.OverallScore =
            (recommendation.CapacityScore * 0.40m) +
            (recommendation.WorkloadScore * 0.30m) +
            (recommendation.ProjectCountScore * 0.20m) +
            (recommendation.SizeScore * 0.10m);

        // Generate recommendation reason
        recommendation.RecommendationReason = GenerateRecommendationReason(recommendation);

        return recommendation;
    }

    private async Task<decimal> CalculateCapacityScore(Squad squad, Project project, ScheduleSuggestion suggestion)
    {
        // Score based on how easily the squad can accommodate the project
        // Perfect fit (uses ~60-80% of capacity) = 100
        // Very tight fit (uses >95%) = lower score
        // Very loose fit (uses <40%) = lower score (might be underutilized)

        var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squad.SquadId);
        if (dailyCapacity == 0) return 0;

        var buffer = suggestion.BufferPercentage;
        var bufferedHours = project.EstimatedDevHours * (1 + buffer / 100);
        var durationDays = suggestion.EstimatedDurationDays;

        if (durationDays == 0) return 50;

        var avgHoursPerDay = bufferedHours / durationDays;
        var utilizationPct = (avgHoursPerDay / dailyCapacity) * 100;

        // Ideal range is 60-80% utilization
        if (utilizationPct >= 60 && utilizationPct <= 80)
            return 100;
        else if (utilizationPct >= 50 && utilizationPct < 60)
            return 90;
        else if (utilizationPct > 80 && utilizationPct <= 95)
            return 80;
        else if (utilizationPct > 95 && utilizationPct <= 100)
            return 60;
        else if (utilizationPct > 100 && utilizationPct <= 120)
            return 40; // Over-allocated but still allowed
        else if (utilizationPct >= 40 && utilizationPct < 50)
            return 70;
        else if (utilizationPct >= 30 && utilizationPct < 40)
            return 60;
        else if (utilizationPct < 30)
            return 50; // Very underutilized
        else
            return 20; // > 120% - should have been rejected
    }

    private async Task<decimal> CalculateWorkloadScore(Squad squad)
    {
        // Score based on current overall utilization
        // Lower utilization = higher score (prefer less busy squads)

        var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squad.SquadId);
        if (dailyCapacity == 0) return 0;

        // Check average utilization over next 30 days
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(30);
        var capacityInfo = await _capacityService.GetSquadCapacityRange(squad.SquadId, startDate, endDate);

        var avgUtilization = capacityInfo.Values
            .Where(c => c.TotalCapacity > 0)
            .Average(c => (c.AllocatedHours / c.TotalCapacity) * 100);

        // Lower utilization = higher score
        if (avgUtilization < 40)
            return 100; // Very available
        else if (avgUtilization >= 40 && avgUtilization < 60)
            return 80; // Good availability
        else if (avgUtilization >= 60 && avgUtilization < 80)
            return 60; // Moderate availability
        else if (avgUtilization >= 80 && avgUtilization < 100)
            return 40; // Busy
        else
            return 20; // Very busy
    }

    private async Task<decimal> CalculateProjectCountScore(Squad squad)
    {
        // Score based on number of active projects
        // Fewer projects = higher score (avoid over-extending squads)

        var activeProjects = await _context.ProjectAllocations
            .Where(pa => pa.SquadId == squad.SquadId)
            .Select(pa => pa.ProjectId)
            .Distinct()
            .CountAsync();

        if (activeProjects == 0)
            return 100; // No projects - fully available
        else if (activeProjects == 1)
            return 80; // One project
        else if (activeProjects == 2)
            return 60; // Two projects
        else if (activeProjects == 3)
            return 40; // Three projects
        else
            return 20; // 4+ projects - very busy
    }

    private async Task<decimal> CalculateSizeScore(Squad squad, Project project)
    {
        // Score based on squad size relative to project size
        // Match larger squads to larger projects

        var teamSize = await _context.TeamMembers
            .Where(tm => tm.SquadId == squad.SquadId && tm.IsActive)
            .CountAsync();

        var projectSize = project.EstimatedDevHours;

        // Categorize project size
        bool isSmallProject = projectSize < 200;
        bool isMediumProject = projectSize >= 200 && projectSize < 500;
        bool isLargeProject = projectSize >= 500;

        // Categorize squad size
        bool isSmallSquad = teamSize <= 5;
        bool isMediumSquad = teamSize > 5 && teamSize <= 10;
        bool isLargeSquad = teamSize > 10;

        // Perfect matches
        if ((isSmallProject && isSmallSquad) ||
            (isMediumProject && isMediumSquad) ||
            (isLargeProject && isLargeSquad))
            return 100;

        // Good matches (one size off)
        if ((isSmallProject && isMediumSquad) ||
            (isMediumProject && (isSmallSquad || isLargeSquad)) ||
            (isLargeProject && isMediumSquad))
            return 70;

        // Poor matches
        return 40;
    }

    private string GenerateRecommendationReason(SquadRecommendation rec)
    {
        var reasons = new List<string>();

        // Capacity
        if (rec.CapacityScore >= 90)
            reasons.Add("Excellent capacity match");
        else if (rec.CapacityScore >= 70)
            reasons.Add("Good capacity fit");
        else if (rec.CapacityScore >= 50)
            reasons.Add("Adequate capacity");
        else
            reasons.Add("Tight capacity");

        // Workload
        if (rec.WorkloadScore >= 80)
            reasons.Add("low current workload");
        else if (rec.WorkloadScore >= 60)
            reasons.Add("moderate workload");
        else
            reasons.Add("high workload");

        // Projects
        if (rec.ProjectCountScore >= 80)
            reasons.Add("few active projects");
        else if (rec.ProjectCountScore >= 60)
            reasons.Add("manageable project count");
        else
            reasons.Add("many active projects");

        return string.Join(", ", reasons);
    }
}
