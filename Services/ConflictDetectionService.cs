using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services;

public class ConflictDetectionService : IConflictDetectionService
{
    private readonly ProjectSchedulerDbContext _context;
    private readonly ICapacityService _capacityService;

    public ConflictDetectionService(
        ProjectSchedulerDbContext context,
        ICapacityService capacityService)
    {
        _context = context;
        _capacityService = capacityService;
    }

    public async Task<ConflictCheckResult> CheckAllocationConflicts(
        int projectId,
        int squadId,
        DateTime startDate,
        DateTime endDate,
        decimal estimatedHours)
    {
        var result = new ConflictCheckResult();

        // Get daily capacity for the squad using GetSquadCapacityRange
        var capacityRange = await _capacityService.GetSquadCapacityRange(squadId, startDate, endDate);

        // Convert to Dictionary<DateOnly, decimal> for easier lookup
        var dailyCapacityData = capacityRange.ToDictionary(
            kvp => DateOnly.FromDateTime(kvp.Key),
            kvp => kvp.Value.TotalCapacity);

        // Calculate average daily hours needed
        var workingDays = GetWorkingDays(startDate, endDate);
        var avgDailyHours = workingDays > 0 ? estimatedHours / workingDays : 0;

        // Check 1: Capacity Overload (>100%)
        await CheckCapacityOverload(result, squadId, startDate, endDate, avgDailyHours, dailyCapacityData);

        // Check 2: High Utilization (80-100%)
        await CheckHighUtilization(result, squadId, startDate, endDate, avgDailyHours, dailyCapacityData);

        // Check 3: Overlapping Onsite Schedules
        await CheckOnsiteOverlap(result, projectId, squadId, startDate, endDate);

        // Set summary and confirmation requirement
        if (result.Conflicts.Any())
        {
            result.HasConflicts = true;
            result.RequiresConfirmation = true;
            result.SummaryMessage = GenerateSummaryMessage(result.Conflicts);
        }

        return result;
    }

    public async Task<ConflictCheckResult> CheckScheduleSuggestionConflicts(
        int projectId,
        int squadId,
        ScheduleSuggestion suggestion)
    {
        if (!suggestion.CanAllocate)
        {
            return new ConflictCheckResult { HasConflicts = false };
        }

        // Check conflicts for development period
        var result = await CheckAllocationConflicts(
            projectId,
            squadId,
            suggestion.SuggestedStartDate,
            suggestion.EstimatedUatDate,
            suggestion.BufferedDevHours);

        // Additional check for onsite periods (UAT and Go-Live)
        var uatWeekStart = GetMondayOfWeek(suggestion.EstimatedUatDate);
        await CheckOnsiteOverlap(result, projectId, squadId, uatWeekStart, uatWeekStart.AddDays(4));

        var goLiveWeekStart = GetMondayOfWeek(suggestion.EstimatedGoLiveDate);
        await CheckOnsiteOverlap(result, projectId, squadId, goLiveWeekStart, goLiveWeekStart.AddDays(4));

        if (result.Conflicts.Any())
        {
            result.HasConflicts = true;
            result.RequiresConfirmation = true;
            result.SummaryMessage = GenerateSummaryMessage(result.Conflicts);
        }

        return result;
    }

    private async Task CheckCapacityOverload(
        ConflictCheckResult result,
        int squadId,
        DateTime startDate,
        DateTime endDate,
        decimal avgDailyHours,
        Dictionary<DateOnly, decimal> capacityData)
    {
        var overloadDates = new List<DateTime>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var dateKey = DateOnly.FromDateTime(currentDate);
                var dailyCapacity = capacityData.ContainsKey(dateKey) ? capacityData[dateKey] : 0;

                // Get existing allocations for this date
                var existingAllocations = await _context.ProjectAllocations
                    .Where(a => a.SquadId == squadId && a.AllocationDate == dateKey)
                    .SumAsync(a => a.AllocatedHours);

                var projectedUtilization = dailyCapacity > 0
                    ? ((existingAllocations + avgDailyHours) / dailyCapacity) * 100
                    : 0;

                if (projectedUtilization > 100)
                {
                    overloadDates.Add(currentDate);
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        if (overloadDates.Any())
        {
            var conflict = new AllocationConflict
            {
                ConflictType = "CapacityOverload",
                Severity = "Critical",
                Message = $"This allocation would exceed capacity on {overloadDates.Count} day(s)",
                Details = $"Capacity would be overloaded between {overloadDates.First():MMM dd} and {overloadDates.Last():MMM dd}. " +
                         $"Consider reducing project scope, extending timeline, or adding team members.",
                ConflictDate = overloadDates.First()
            };
            result.Conflicts.Add(conflict);
        }
    }

    private async Task CheckHighUtilization(
        ConflictCheckResult result,
        int squadId,
        DateTime startDate,
        DateTime endDate,
        decimal avgDailyHours,
        Dictionary<DateOnly, decimal> capacityData)
    {
        var highUtilDates = new List<(DateTime Date, decimal Utilization)>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var dateKey = DateOnly.FromDateTime(currentDate);
                var dailyCapacity = capacityData.ContainsKey(dateKey) ? capacityData[dateKey] : 0;

                var existingAllocations = await _context.ProjectAllocations
                    .Where(a => a.SquadId == squadId && a.AllocationDate == dateKey)
                    .SumAsync(a => a.AllocatedHours);

                var projectedUtilization = dailyCapacity > 0
                    ? ((existingAllocations + avgDailyHours) / dailyCapacity) * 100
                    : 0;

                if (projectedUtilization >= 80 && projectedUtilization <= 100)
                {
                    highUtilDates.Add((currentDate, projectedUtilization));
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        if (highUtilDates.Any())
        {
            var maxUtil = highUtilDates.Max(d => d.Utilization);
            var conflict = new AllocationConflict
            {
                ConflictType = "HighUtilization",
                Severity = "Warning",
                Message = $"High utilization detected: up to {maxUtil:F0}% capacity",
                Details = $"Squad will be at {maxUtil:F0}% capacity on {highUtilDates.Count} day(s). " +
                         $"This leaves minimal buffer for unexpected issues or delays.",
                ProjectedUtilization = maxUtil,
                ConflictDate = highUtilDates.First().Date
            };
            result.Conflicts.Add(conflict);
        }
    }

    private async Task CheckOnsiteOverlap(
        ConflictCheckResult result,
        int projectId,
        int squadId,
        DateTime startDate,
        DateTime endDate)
    {
        var searchStartDate = startDate.AddDays(-7);
        var searchEndDate = endDate.AddDays(7);

        // Find all projects allocated to this squad with onsite schedules in the date range
        // We need to find projects that have allocations to this squad
        var projectsInSquad = await _context.ProjectAllocations
            .Where(pa => pa.SquadId == squadId && pa.ProjectId != projectId)
            .Select(pa => pa.ProjectId)
            .Distinct()
            .ToListAsync();

        var overlappingOnsite = await _context.OnsiteSchedules
            .Include(os => os.Project)
            .Where(os => projectsInSquad.Contains(os.ProjectId))
            .ToListAsync();

        // Filter by date range in memory
        overlappingOnsite = overlappingOnsite
            .Where(os => os.StartDate >= searchStartDate && os.StartDate <= searchEndDate)
            .ToList();

        if (overlappingOnsite.Any())
        {
            var conflictingProjects = overlappingOnsite
                .Select(os => $"{os.Project.ProjectNumber} ({os.OnsiteType})")
                .Distinct()
                .ToList();

            var conflict = new AllocationConflict
            {
                ConflictType = "OnsiteOverlap",
                Severity = "Warning",
                Message = $"Overlapping onsite schedules detected with {overlappingOnsite.Count} other project(s)",
                Details = $"The following projects have onsite schedules during this period: {string.Join(", ", conflictingProjects)}. " +
                         $"Ensure adequate onsite resources are available.",
                ConflictWeekStart = overlappingOnsite.First().StartDate,
                ConflictingProjects = conflictingProjects
            };
            result.Conflicts.Add(conflict);
        }
    }

    private string GenerateSummaryMessage(List<AllocationConflict> conflicts)
    {
        var criticalCount = conflicts.Count(c => c.Severity == "Critical");
        var warningCount = conflicts.Count(c => c.Severity == "Warning");

        if (criticalCount > 0 && warningCount > 0)
        {
            return $"Found {criticalCount} critical issue(s) and {warningCount} warning(s). Review conflicts before proceeding.";
        }
        else if (criticalCount > 0)
        {
            return $"Found {criticalCount} critical issue(s). This allocation may cause capacity problems.";
        }
        else
        {
            return $"Found {warningCount} warning(s). Review before proceeding.";
        }
    }

    private int GetWorkingDays(DateTime startDate, DateTime endDate)
    {
        var workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    private DateTime GetMondayOfWeek(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-daysToSubtract).Date;
    }
}
