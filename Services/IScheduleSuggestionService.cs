using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services
{
    public interface IScheduleSuggestionService
    {
        Task<ScheduleSuggestion> GetScheduleSuggestion(
            int projectId,
            int squadId,
            decimal? bufferPercentage = null,
            string? algorithmType = null,
            DateTime? startDate = null);
        Task<bool> ApplyScheduleSuggestion(int projectId, int squadId, ScheduleSuggestion suggestion);
        Task<List<AlgorithmComparison>> CompareAlgorithms(
            int projectId,
            int squadId,
            decimal? bufferPercentage = null,
            DateTime? startDate = null);
    }

    // Helper class to track flexible allocation schedule
    public class AllocationSchedule
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<DateTime, decimal> DailyAllocations { get; set; } = new();
        public decimal TotalHoursAllocated { get; set; }
    }

    public class ScheduleSuggestion
    {
        public int ProjectId { get; set; }
        public int SquadId { get; set; }
        public string SquadName { get; set; } = string.Empty;
        public DateTime SuggestedStartDate { get; set; }
        public DateTime EstimatedCodeCompleteDate { get; set; }
        public DateTime EstimatedCrpDate { get; set; }
        public DateTime EstimatedUatDate { get; set; }
        public DateTime EstimatedGoLiveDate { get; set; }
        public decimal OriginalDevHours { get; set; }
        public decimal BufferPercentage { get; set; }
        public decimal BufferedDevHours { get; set; }
        public int EstimatedDurationDays { get; set; }
        public bool CanAllocate { get; set; }
        public string Message { get; set; } = string.Empty;

        // Flexible allocation schedule - maps date to hours allocated
        public Dictionary<DateTime, decimal>? AllocationSchedule { get; set; }
    }
}