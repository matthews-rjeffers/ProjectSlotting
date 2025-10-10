using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services
{
    public class ScheduleSuggestionService : IScheduleSuggestionService
    {
        private readonly ProjectSchedulerDbContext _context;
        private readonly ICapacityService _capacityService;
        private readonly IAllocationService _allocationService;

        public ScheduleSuggestionService(
            ProjectSchedulerDbContext context,
            ICapacityService capacityService,
            IAllocationService allocationService)
        {
            _context = context;
            _capacityService = capacityService;
            _allocationService = allocationService;
        }

        public async Task<ScheduleSuggestion> GetScheduleSuggestion(int projectId, int squadId, decimal? bufferPercentage = null)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return new ScheduleSuggestion
                {
                    ProjectId = projectId,
                    SquadId = squadId,
                    CanAllocate = false,
                    Message = "Project not found"
                };
            }

            var squad = await _context.Squads.FindAsync(squadId);
            if (squad == null)
            {
                return new ScheduleSuggestion
                {
                    ProjectId = projectId,
                    SquadId = squadId,
                    CanAllocate = false,
                    Message = "Squad not found"
                };
            }

            // Use provided buffer or project's buffer or default to 20%
            var buffer = bufferPercentage ?? project.BufferPercentage;
            if (buffer == 0) buffer = 20;

            // Calculate buffered hours
            var originalHours = project.EstimatedDevHours;
            var bufferedHours = originalHours * (1 + buffer / 100);

            // Get squad's daily capacity
            var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squadId);
            if (dailyCapacity <= 0)
            {
                return new ScheduleSuggestion
                {
                    ProjectId = projectId,
                    SquadId = squadId,
                    SquadName = squad.SquadName,
                    CanAllocate = false,
                    Message = "Squad has no available capacity"
                };
            }

            // Find the earliest available start date
            var searchDate = DateTime.Today.AddDays(1); // Start from tomorrow
            var maxSearchDays = 365; // Look up to 1 year ahead
            var searchEndDate = searchDate.AddDays(maxSearchDays);

            DateTime? suggestedStartDate = null;
            DateTime? estimatedEndDate = null;

            while (searchDate < searchEndDate && suggestedStartDate == null)
            {
                // Skip weekends
                if (searchDate.DayOfWeek == DayOfWeek.Saturday || searchDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    searchDate = searchDate.AddDays(1);
                    continue;
                }

                // Check if we can allocate starting from this date
                var canStart = await TryFindAllocationWindow(squadId, searchDate, bufferedHours, dailyCapacity);

                if (canStart.HasValue)
                {
                    suggestedStartDate = searchDate;
                    estimatedEndDate = canStart.Value;
                    break;
                }

                searchDate = searchDate.AddDays(1);
            }

            if (suggestedStartDate == null)
            {
                return new ScheduleSuggestion
                {
                    ProjectId = projectId,
                    SquadId = squadId,
                    SquadName = squad.SquadName,
                    OriginalDevHours = originalHours,
                    BufferPercentage = buffer,
                    BufferedDevHours = bufferedHours,
                    CanAllocate = false,
                    Message = "No available capacity found within the next year"
                };
            }

            // Calculate dates based on the found window
            // We know estimatedEndDate has a value because we checked for null above
            // The estimatedEndDate is when ALL dev hours (with buffer) are allocated
            // Timeline: Start -> [dev work with buffer] -> CRP -> [~1 week] -> UAT -> [2 weeks] -> Go-Live
            // Dev hours are allocated through UAT date (includes final testing/fixes)
            var devCompleteDate = estimatedEndDate!.Value;

            // CRP is when code is ready for production testing - very close to UAT start
            // Let's set CRP about 3-5 working days before we'd start UAT
            var crpDate = AddWorkingDays(devCompleteDate, -3); // 3 days before end of dev work

            // UAT starts right around when dev work completes
            var uatDate = devCompleteDate;

            // Go-Live is 2 weeks after UAT
            var goLiveDate = AddWorkingDays(uatDate, 10);

            // Calculate working days duration
            var durationDays = GetWorkingDays(suggestedStartDate.Value, estimatedEndDate.Value);

            return new ScheduleSuggestion
            {
                ProjectId = projectId,
                SquadId = squadId,
                SquadName = squad.SquadName,
                SuggestedStartDate = suggestedStartDate.Value,
                EstimatedCrpDate = crpDate,
                EstimatedUatDate = uatDate,
                EstimatedGoLiveDate = goLiveDate,
                OriginalDevHours = originalHours,
                BufferPercentage = buffer,
                BufferedDevHours = bufferedHours,
                EstimatedDurationDays = durationDays,
                CanAllocate = true,
                Message = $"Project can be scheduled starting {suggestedStartDate.Value:MMM dd, yyyy}"
            };
        }

        public async Task<bool> ApplyScheduleSuggestion(int projectId, int squadId, ScheduleSuggestion suggestion)
        {
            if (!suggestion.CanAllocate)
                return false;

            var project = await _context.Projects
                .Include(p => p.OnsiteSchedules)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                return false;

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Update project dates
                project.StartDate = suggestion.SuggestedStartDate;
                project.Crpdate = suggestion.EstimatedCrpDate;
                project.Uatdate = suggestion.EstimatedUatDate;
                project.GoLiveDate = suggestion.EstimatedGoLiveDate;
                project.BufferPercentage = suggestion.BufferPercentage;
                project.UpdatedDate = DateTime.UtcNow;

                // Use buffered hours for allocation (but don't save it to EstimatedDevHours)
                // Allocate through UAT date since that's when all dev work (including final testing) completes
                var allocationSuccess = await _allocationService.AllocateProjectToSquad(
                    projectId,
                    squadId,
                    suggestion.SuggestedStartDate,
                    suggestion.EstimatedUatDate,
                    suggestion.BufferedDevHours
                );

                if (!allocationSuccess)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Clear existing onsite schedules for this project
                var existingSchedules = await _context.OnsiteSchedules
                    .Where(s => s.ProjectId == projectId)
                    .ToListAsync();
                _context.OnsiteSchedules.RemoveRange(existingSchedules);

                // Create UAT onsite schedule (1 engineer for 1 week)
                var uatWeekStart = GetMondayOfWeek(suggestion.EstimatedUatDate);
                var uatSchedule = new OnsiteSchedule
                {
                    ProjectId = projectId,
                    WeekStartDate = uatWeekStart,
                    EngineerCount = 1,
                    OnsiteType = "UAT",
                    CreatedDate = DateTime.UtcNow
                };
                _context.OnsiteSchedules.Add(uatSchedule);

                // Create Go-Live onsite schedule (1 engineer for 1 week)
                var goLiveWeekStart = GetMondayOfWeek(suggestion.EstimatedGoLiveDate);
                var goLiveSchedule = new OnsiteSchedule
                {
                    ProjectId = projectId,
                    WeekStartDate = goLiveWeekStart,
                    EngineerCount = 1,
                    OnsiteType = "GoLive",
                    CreatedDate = DateTime.UtcNow
                };
                _context.OnsiteSchedules.Add(goLiveSchedule);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error applying schedule suggestion: {ex.Message}");
                return false;
            }
        }

        private async Task<DateTime?> TryFindAllocationWindow(int squadId, DateTime startDate, decimal totalHours, decimal dailyCapacity)
        {
            var currentDate = startDate;
            var hoursAllocated = 0m;
            var maxDays = 365; // Maximum project duration
            var daysChecked = 0;

            while (hoursAllocated < totalHours && daysChecked < maxDays)
            {
                // Skip weekends
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Get remaining capacity for this day
                var remainingCapacity = await _capacityService.GetSquadRemainingCapacity(squadId, currentDate);

                // We need consistent daily allocation, so check if we have enough capacity
                var dailyHoursNeeded = Math.Min(dailyCapacity, totalHours - hoursAllocated);

                if (remainingCapacity >= dailyHoursNeeded)
                {
                    hoursAllocated += dailyHoursNeeded;
                }
                else
                {
                    // If we can't allocate on this day, this window doesn't work
                    // Reset and return null to try a different start date
                    return null;
                }

                if (hoursAllocated >= totalHours)
                {
                    return currentDate; // Found complete window
                }

                currentDate = currentDate.AddDays(1);
                daysChecked++;
            }

            return null; // Couldn't find a complete window
        }

        private DateTime AddWorkingDays(DateTime startDate, int workingDays)
        {
            var date = startDate;
            var direction = workingDays >= 0 ? 1 : -1;
            var daysToAdd = Math.Abs(workingDays);
            var daysAdded = 0;

            while (daysAdded < daysToAdd)
            {
                date = date.AddDays(direction);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    daysAdded++;
                }
            }

            return date;
        }

        private int GetWorkingDays(DateTime startDate, DateTime endDate)
        {
            var days = 0;
            var date = startDate.Date;

            while (date <= endDate.Date)
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    days++;
                }
                date = date.AddDays(1);
            }

            return days;
        }

        private DateTime GetMondayOfWeek(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7
            return date.AddDays(1 - dayOfWeek);
        }
    }
}