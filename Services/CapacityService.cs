using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;

namespace ProjectScheduler.Services
{
    public class CapacityService : ICapacityService
    {
        private readonly ProjectSchedulerDbContext _context;

        public CapacityService(ProjectSchedulerDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetSquadDailyCapacity(int squadId)
        {
            var capacity = await _context.TeamMembers
                .Where(tm => tm.SquadId == squadId && tm.IsActive)
                .SumAsync(tm => tm.DailyCapacityHours);

            return capacity;
        }

        public async Task<decimal> GetSquadAllocatedHours(int squadId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var allocated = await _context.ProjectAllocations
                .Where(pa => pa.SquadId == squadId && pa.AllocationDate == dateOnly)
                .SumAsync(pa => pa.AllocatedHours);

            return allocated;
        }

        public async Task<decimal> GetSquadRemainingCapacity(int squadId, DateTime date)
        {
            var totalCapacity = await GetSquadDailyCapacity(squadId);
            var allocatedHours = await GetSquadAllocatedHours(squadId, date);

            return totalCapacity - allocatedHours;
        }

        public async Task<Dictionary<DateTime, CapacityInfo>> GetSquadCapacityRange(int squadId, DateTime startDate, DateTime endDate)
        {
            var totalCapacity = await GetSquadDailyCapacity(squadId);

            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate);

            // Get all allocations in the date range
            var allocations = await _context.ProjectAllocations
                .Where(pa => pa.SquadId == squadId &&
                            pa.AllocationDate >= startDateOnly &&
                            pa.AllocationDate <= endDateOnly)
                .GroupBy(pa => pa.AllocationDate)
                .Select(g => new { Date = g.Key, AllocatedHours = g.Sum(pa => pa.AllocatedHours) })
                .ToListAsync();

            var result = new Dictionary<DateTime, CapacityInfo>();

            // Generate capacity info for each day in range
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // Skip weekends (optional - can be configured)
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                var dateOnly = DateOnly.FromDateTime(date);
                var allocated = allocations.FirstOrDefault(a => a.Date == dateOnly)?.AllocatedHours ?? 0;

                result[date] = new CapacityInfo
                {
                    Date = date,
                    TotalCapacity = totalCapacity,
                    AllocatedHours = allocated,
                    RemainingCapacity = totalCapacity - allocated
                };
            }

            return result;
        }
    }
}
