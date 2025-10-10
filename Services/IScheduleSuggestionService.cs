using System;
using System.Threading.Tasks;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services
{
    public interface IScheduleSuggestionService
    {
        Task<ScheduleSuggestion> GetScheduleSuggestion(int projectId, int squadId, decimal? bufferPercentage = null);
        Task<bool> ApplyScheduleSuggestion(int projectId, int squadId, ScheduleSuggestion suggestion);
    }

    public class ScheduleSuggestion
    {
        public int ProjectId { get; set; }
        public int SquadId { get; set; }
        public string SquadName { get; set; } = string.Empty;
        public DateTime SuggestedStartDate { get; set; }
        public DateTime EstimatedCrpDate { get; set; }
        public DateTime EstimatedUatDate { get; set; }
        public DateTime EstimatedGoLiveDate { get; set; }
        public decimal OriginalDevHours { get; set; }
        public decimal BufferPercentage { get; set; }
        public decimal BufferedDevHours { get; set; }
        public int EstimatedDurationDays { get; set; }
        public bool CanAllocate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}