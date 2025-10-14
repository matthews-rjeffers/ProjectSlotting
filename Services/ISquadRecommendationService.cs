using ProjectScheduler.Models;

namespace ProjectScheduler.Services;

public interface ISquadRecommendationService
{
    Task<List<SquadRecommendation>> GetSquadRecommendations(
        int projectId,
        decimal? bufferPercentage = null,
        string? algorithmType = null,
        DateTime? startDate = null);
}
