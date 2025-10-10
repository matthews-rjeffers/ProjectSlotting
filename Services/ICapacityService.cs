using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectScheduler.Services
{
    public interface ICapacityService
    {
        Task<decimal> GetSquadDailyCapacity(int squadId);
        Task<decimal> GetSquadAllocatedHours(int squadId, DateTime date);
        Task<decimal> GetSquadRemainingCapacity(int squadId, DateTime date);
        Task<Dictionary<DateTime, CapacityInfo>> GetSquadCapacityRange(int squadId, DateTime startDate, DateTime endDate);
    }

    public class CapacityInfo
    {
        public DateTime Date { get; set; }
        public decimal TotalCapacity { get; set; }
        public decimal AllocatedHours { get; set; }
        public decimal RemainingCapacity { get; set; }
    }
}
