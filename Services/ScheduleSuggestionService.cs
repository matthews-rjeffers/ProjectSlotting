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

        public async Task<ScheduleSuggestion> GetScheduleSuggestion(
            int projectId,
            int squadId,
            decimal? bufferPercentage = null,
            string? algorithmType = null,
            DateTime? startDate = null)
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

            // Use provided start date or default to tomorrow
            var searchDate = startDate ?? DateTime.Today.AddDays(1);

            // Default to greedy algorithm if not specified
            var useGreedy = string.IsNullOrEmpty(algorithmType) || algorithmType.ToLower() == "greedy";

            Console.WriteLine($"[SCHEDULE SUGGESTION] Using {(useGreedy ? "Greedy" : "Strict")} algorithm, starting from {searchDate:yyyy-MM-dd}");

            AllocationSchedule? schedule = null;
            DateTime? estimatedEndDate = null;

            if (useGreedy)
            {
                // Try flexible/greedy allocation
                schedule = await TryFindFlexibleAllocationWindow(squadId, searchDate, bufferedHours, dailyCapacity);
            }
            else
            {
                // Try strict allocation
                estimatedEndDate = await TryFindAllocationWindow(squadId, searchDate, bufferedHours, dailyCapacity);
            }

            // Check if we found a schedule (either greedy or strict)
            if (schedule == null && estimatedEndDate == null)
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

            // Determine the dates based on which algorithm was used
            DateTime actualStartDate;
            DateTime actualEndDate;
            Dictionary<DateTime, decimal>? allocationSchedule = null;

            if (useGreedy && schedule != null)
            {
                actualStartDate = schedule.StartDate;
                actualEndDate = schedule.EndDate;
                allocationSchedule = schedule.DailyAllocations;
            }
            else
            {
                actualStartDate = searchDate;
                actualEndDate = estimatedEndDate!.Value;
            }

            // Calculate dates based on the found schedule
            // The EndDate is when ALL dev hours (with buffer) are allocated
            // Timeline: Start -> [dev work with buffer] -> CRP -> [~1 week] -> UAT -> [2 weeks] -> Go-Live
            // Dev hours are allocated through UAT date (includes final testing/fixes)
            var devCompleteDate = actualEndDate;

            // CRP is when code is ready for production testing - very close to UAT start
            // Let's set CRP about 3-5 working days before we'd start UAT
            var crpDate = AddWorkingDays(devCompleteDate, -3); // 3 days before end of dev work

            // UAT starts right around when dev work completes
            var uatDate = devCompleteDate;

            // Go-Live is 2 weeks after UAT
            var goLiveDate = AddWorkingDays(uatDate, 10);

            // Calculate working days duration
            var durationDays = GetWorkingDays(actualStartDate, actualEndDate);

            var message = useGreedy
                ? $"Project can be scheduled starting {actualStartDate:MMM dd, yyyy} (using Greedy Algorithm over {allocationSchedule?.Count ?? durationDays} days)"
                : $"Project can be scheduled starting {actualStartDate:MMM dd, yyyy} (using Strict Algorithm over {durationDays} days)";

            return new ScheduleSuggestion
            {
                ProjectId = projectId,
                SquadId = squadId,
                SquadName = squad.SquadName,
                SuggestedStartDate = actualStartDate,
                EstimatedCrpDate = crpDate,
                EstimatedUatDate = uatDate,
                EstimatedGoLiveDate = goLiveDate,
                OriginalDevHours = originalHours,
                BufferPercentage = buffer,
                BufferedDevHours = bufferedHours,
                EstimatedDurationDays = durationDays,
                CanAllocate = true,
                Message = message,
                AllocationSchedule = allocationSchedule
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

                // Use flexible allocation schedule if available, otherwise fall back to uniform allocation
                if (suggestion.AllocationSchedule != null && suggestion.AllocationSchedule.Any())
                {
                    Console.WriteLine($"[APPLY SCHEDULE] Using flexible allocation with {suggestion.AllocationSchedule.Count} days");

                    // Create allocations based on the flexible schedule
                    foreach (var (date, hours) in suggestion.AllocationSchedule)
                    {
                        var allocation = new ProjectAllocation
                        {
                            ProjectId = projectId,
                            SquadId = squadId,
                            AllocationDate = DateOnly.FromDateTime(date),
                            AllocatedHours = hours,
                            AllocationType = "Development",
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.ProjectAllocations.Add(allocation);
                        Console.WriteLine($"[APPLY SCHEDULE] {date:yyyy-MM-dd}: {hours}h");
                    }
                }
                else
                {
                    Console.WriteLine($"[APPLY SCHEDULE] Using uniform allocation");
                    // Fall back to uniform allocation
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

        // New flexible allocation algorithm that spreads hours across partial capacity
        private async Task<AllocationSchedule?> TryFindFlexibleAllocationWindow(
            int squadId,
            DateTime startDate,
            decimal totalHours,
            decimal dailyCapacity,
            decimal minHoursPerDay = 2m,  // Don't allocate less than 2 hours/day
            int maxDurationDays = 180)    // Don't stretch project beyond 180 working days
        {
            var currentDate = startDate;
            var hoursAllocated = 0m;
            var schedule = new AllocationSchedule { StartDate = startDate };
            var workingDaysUsed = 0;

            Console.WriteLine($"[FLEXIBLE SCHEDULE] Starting search from {startDate:yyyy-MM-dd} for {totalHours}h");

            while (hoursAllocated < totalHours && workingDaysUsed < maxDurationDays)
            {
                // Skip weekends
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Get remaining capacity for this day
                var remainingCapacity = await _capacityService.GetSquadRemainingCapacity(squadId, currentDate);

                // Calculate how much we want to allocate
                var hoursRemaining = totalHours - hoursAllocated;
                var idealHours = Math.Min(dailyCapacity, hoursRemaining);

                // Take whatever capacity is available (up to what we need)
                var hoursToAllocate = Math.Min(remainingCapacity, idealHours);

                Console.WriteLine($"[FLEXIBLE SCHEDULE] {currentDate:yyyy-MM-dd}: Remaining capacity={remainingCapacity}h, Ideal={idealHours}h, Allocating={hoursToAllocate}h");

                // Only allocate if we meet minimum threshold OR it's the last bit we need
                if (hoursToAllocate >= minHoursPerDay || hoursToAllocate >= hoursRemaining)
                {
                    if (hoursToAllocate > 0)
                    {
                        schedule.DailyAllocations[currentDate] = hoursToAllocate;
                        hoursAllocated += hoursToAllocate;
                        workingDaysUsed++;
                    }
                }

                if (hoursAllocated >= totalHours)
                {
                    schedule.EndDate = currentDate;
                    schedule.TotalHoursAllocated = hoursAllocated;
                    Console.WriteLine($"[FLEXIBLE SCHEDULE] Successfully allocated {hoursAllocated}h over {workingDaysUsed} working days");
                    return schedule; // Successfully allocated all hours
                }

                currentDate = currentDate.AddDays(1);
            }

            // Couldn't allocate all hours within constraints
            Console.WriteLine($"[FLEXIBLE SCHEDULE] Failed to allocate all hours. Allocated {hoursAllocated}/{totalHours}h over {workingDaysUsed} days");
            return null;
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