using ProjectScheduler.Models;

namespace ProjectScheduler.Services;

public interface IConflictDetectionService
{
    /// <summary>
    /// Check for conflicts when allocating a project to a squad
    /// </summary>
    Task<ConflictCheckResult> CheckAllocationConflicts(
        int projectId,
        int squadId,
        DateTime startDate,
        DateTime endDate,
        decimal estimatedHours);

    /// <summary>
    /// Check for conflicts when applying a schedule suggestion
    /// </summary>
    Task<ConflictCheckResult> CheckScheduleSuggestionConflicts(
        int projectId,
        int squadId,
        ScheduleSuggestion suggestion);
}
