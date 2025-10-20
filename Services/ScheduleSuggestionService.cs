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

            // NEW LOGIC: Preserve existing dates, use week-based calculations
            // Use provided start date or project's start date or default to tomorrow
            var searchDate = startDate ?? project.StartDate ?? DateTime.Today.AddDays(1);

            // Determine which algorithm to use
            var algoType = (algorithmType ?? "strict").ToLower();
            var useGreedy = algoType == "greedy";
            var useDelayed = algoType == "delayed";
            var algorithmName = useGreedy ? "Greedy" : useDelayed ? "Delayed" : "Strict";

            Console.WriteLine($"[SCHEDULE SUGGESTION] Using {algorithmName} algorithm");

            AllocationSchedule? schedule = null;
            DateTime actualGoLiveDate;
            DateTime actualUatDate;
            DateTime actualCrpDate;
            DateTime actualCodeCompleteDate;

            // Step 1: Determine dates based on what's already set, using week-based calculations
            if (project.GoLiveDate.HasValue)
            {
                // Go Live is set - work BACKWARDS using week-based offsets
                actualGoLiveDate = project.GoLiveDate.Value;

                // UAT = Monday of week before Go Live's week
                if (project.Uatdate.HasValue)
                {
                    actualUatDate = project.Uatdate.Value;
                }
                else
                {
                    actualUatDate = GetMondayOfPreviousWeek(actualGoLiveDate);
                }

                // CRP = Monday of week before UAT's week
                if (project.Crpdate.HasValue)
                {
                    actualCrpDate = project.Crpdate.Value;
                }
                else
                {
                    actualCrpDate = GetMondayOfPreviousWeek(actualUatDate);
                }

                // Code Complete = CRP (if not set)
                if (project.CodeCompleteDate.HasValue)
                {
                    actualCodeCompleteDate = project.CodeCompleteDate.Value;
                }
                else
                {
                    actualCodeCompleteDate = actualCrpDate;
                }

                if (useGreedy)
                {
                    // Greedy: Use all remaining capacity but respect Go-Live date
                    schedule = await TryFindFlexibleAllocationWindow(squadId, searchDate, bufferedHours, dailyCapacity, actualUatDate);
                }
                else if (useDelayed)
                {
                    // Delayed: Reverse greedy - start from UAT and work backwards
                    schedule = await TryFindDelayedAllocationWindow(squadId, actualUatDate, bufferedHours, dailyCapacity);
                }
                else
                {
                    // Strict: Distribute hours evenly across timeframe
                    schedule = await TryFindEvenAllocationWindow(squadId, searchDate, bufferedHours, dailyCapacity, actualUatDate);
                }

                if (schedule == null)
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
                        Message = $"Cannot allocate {bufferedHours}h before Go-Live date {actualGoLiveDate:MMM dd, yyyy} - would exceed 120% capacity limit"
                    };
                }
            }
            else
            {
                // No Go-Live date - work FORWARD
                // NEW: Calculate CRP→UAT hours with sliding scale (max 40 hours)
                var crpToUatHours = Math.Min(bufferedHours * 0.1m, 40m);
                var startToCodeCompleteHours = bufferedHours - crpToUatHours;

                if (useGreedy)
                {
                    // Try flexible/greedy allocation for Start→CodeComplete phase
                    schedule = await TryFindFlexibleAllocationWindow(squadId, searchDate, startToCodeCompleteHours, dailyCapacity);
                }
                else if (useDelayed)
                {
                    // For Delayed without Go-Live, calculate estimated date first
                    var estimatedDays = (int)Math.Ceiling(startToCodeCompleteHours / dailyCapacity);
                    var estimatedEnd = AddWorkingDays(searchDate, estimatedDays);
                    schedule = await TryFindDelayedAllocationWindow(squadId, estimatedEnd, startToCodeCompleteHours, dailyCapacity);
                }
                else
                {
                    // Try strict (even) allocation - estimate end date first
                    var estimatedDays = (int)Math.Ceiling(startToCodeCompleteHours / dailyCapacity);
                    var estimatedEnd = AddWorkingDays(searchDate, estimatedDays);
                    schedule = await TryFindEvenAllocationWindow(squadId, searchDate, startToCodeCompleteHours, dailyCapacity, estimatedEnd);
                }

                // Check if we found a schedule for Start→CodeComplete phase
                if (schedule == null)
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
                        Message = "Cannot allocate - would exceed 120% capacity limit"
                    };
                }

                // NEW: Code Complete Date is when Start→CodeComplete phase is done
                actualCodeCompleteDate = schedule.EndDate;

                // Code Complete = CRP (when not set)
                actualCrpDate = actualCodeCompleteDate;

                // UAT = Monday of week after CRP's week
                actualUatDate = GetMondayOfNextWeek(actualCrpDate);

                // Go Live = Monday of week after UAT's week
                actualGoLiveDate = GetMondayOfNextWeek(actualUatDate);
            }

            // Determine the dates from schedule
            DateTime finalStartDate = schedule!.StartDate;
            DateTime finalEndDate = schedule.EndDate;

            // Calculate working days duration
            var durationDays = GetWorkingDays(finalStartDate, finalEndDate);

            var message = project.GoLiveDate.HasValue
                ? $"Project scheduled to meet Go-Live {actualGoLiveDate:MMM dd, yyyy} using {algorithmName} Algorithm over {durationDays} days"
                : (useGreedy
                    ? $"Project can be scheduled starting {finalStartDate:MMM dd, yyyy} (using Greedy Algorithm over {schedule?.DailyAllocations.Count ?? durationDays} days)"
                    : useDelayed
                        ? $"Project can be scheduled starting {finalStartDate:MMM dd, yyyy} (using Delayed Algorithm over {durationDays} days)"
                        : $"Project can be scheduled starting {finalStartDate:MMM dd, yyyy} (using Strict Algorithm over {durationDays} days)");

            return new ScheduleSuggestion
            {
                ProjectId = projectId,
                SquadId = squadId,
                SquadName = squad.SquadName,
                SuggestedStartDate = finalStartDate,
                EstimatedCodeCompleteDate = actualCodeCompleteDate,
                EstimatedCrpDate = actualCrpDate,
                EstimatedUatDate = actualUatDate,
                EstimatedGoLiveDate = actualGoLiveDate,
                OriginalDevHours = originalHours,
                BufferPercentage = buffer,
                BufferedDevHours = bufferedHours,
                EstimatedDurationDays = durationDays,
                CanAllocate = true,
                Message = message,
                AllocationSchedule = schedule?.DailyAllocations
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
                project.CodeCompleteDate = suggestion.EstimatedCodeCompleteDate;
                project.Crpdate = suggestion.EstimatedCrpDate;
                project.Uatdate = suggestion.EstimatedUatDate;
                project.GoLiveDate = suggestion.EstimatedGoLiveDate;
                project.BufferPercentage = suggestion.BufferPercentage;
                project.UpdatedDate = DateTime.UtcNow;

                // NEW: Create sliding scale split allocations with 40-hour cap
                // Phase 1: Bulk dev from Start → Code Complete
                // Phase 2: Polish dev from CRP → UAT (max 40 hours)

                var crpToUatHours = Math.Min(suggestion.BufferedDevHours * 0.1m, 40m);
                var startToCodeCompleteHours = suggestion.BufferedDevHours - crpToUatHours;

                Console.WriteLine($"[APPLY SCHEDULE] === Creating Sliding Scale Split Allocations ===");
                Console.WriteLine($"[APPLY SCHEDULE] Total buffered hours: {suggestion.BufferedDevHours}h");
                Console.WriteLine($"[APPLY SCHEDULE] Start→CodeComplete: {startToCodeCompleteHours}h");
                Console.WriteLine($"[APPLY SCHEDULE] CRP→UAT (max 40h): {crpToUatHours}h");

                // Phase 1: Bulk dev allocation from Start to Code Complete
                var phase1Start = suggestion.SuggestedStartDate;
                var phase1End = suggestion.EstimatedCodeCompleteDate;
                var phase1WorkingDays = GetWorkingDays(phase1Start, phase1End);

                if (phase1WorkingDays <= 0)
                {
                    Console.WriteLine($"[APPLY SCHEDULE] ERROR: No working days from Start to Code Complete");
                    await transaction.RollbackAsync();
                    return false;
                }

                var phase1HoursPerDay = startToCodeCompleteHours / phase1WorkingDays;
                Console.WriteLine($"[APPLY SCHEDULE] Phase 1: {phase1Start:yyyy-MM-dd} to {phase1End:yyyy-MM-dd} ({phase1WorkingDays} days, {phase1HoursPerDay:F2}h/day)");

                var currentDate = phase1Start;
                while (currentDate <= phase1End)
                {
                    if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        var allocation = new ProjectAllocation
                        {
                            ProjectId = projectId,
                            SquadId = squadId,
                            AllocationDate = DateOnly.FromDateTime(currentDate),
                            AllocatedHours = phase1HoursPerDay,
                            AllocationType = "Development",
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.ProjectAllocations.Add(allocation);
                        Console.WriteLine($"[APPLY SCHEDULE] Phase 1: {currentDate:yyyy-MM-dd}: {phase1HoursPerDay:F2}h");
                    }
                    currentDate = currentDate.AddDays(1);
                }

                // Phase 2: Polish dev allocation from CRP to UAT (max 40 hours)
                // Only run if UAT exists AND is AFTER CRP (CRP and UAT can never be same date)
                Console.WriteLine($"[APPLY SCHEDULE] === PHASE 2: POLISH DEV HOURS (CRP → UAT) ===");

                if (suggestion.EstimatedUatDate <= suggestion.EstimatedCrpDate)
                {
                    Console.WriteLine($"[APPLY SCHEDULE] SKIPPED: UAT ({suggestion.EstimatedUatDate:yyyy-MM-dd}) is not after CRP ({suggestion.EstimatedCrpDate:yyyy-MM-dd})");
                }
                else
                {
                    Console.WriteLine($"[APPLY SCHEDULE] Period: {suggestion.EstimatedCrpDate:yyyy-MM-dd} to {suggestion.EstimatedUatDate:yyyy-MM-dd}");

                    // Start Phase 2 day AFTER CRP to avoid overlap with Phase 1
                    var phase2Start = suggestion.EstimatedCrpDate.AddDays(1);
                    var phase2End = suggestion.EstimatedUatDate;
                    var phase2WorkingDays = GetWorkingDays(phase2Start, phase2End);

                    Console.WriteLine($"[APPLY SCHEDULE] Working days: {phase2WorkingDays}");
                    Console.WriteLine($"[APPLY SCHEDULE] CRP→UAT hours (max 40): {crpToUatHours}h");

                    if (phase2WorkingDays <= 0)
                    {
                        Console.WriteLine($"[APPLY SCHEDULE] No working days between CRP and UAT, skipping Phase 2");
                    }
                    else
                    {
                        var phase2HoursPerDay = crpToUatHours / phase2WorkingDays;
                        Console.WriteLine($"[APPLY SCHEDULE] Phase 2 hours per day: {phase2HoursPerDay:F2}h");

                        currentDate = phase2Start;
                        while (currentDate <= phase2End)
                        {
                            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                            {
                                var allocation = new ProjectAllocation
                                {
                                    ProjectId = projectId,
                                    SquadId = squadId,
                                    AllocationDate = DateOnly.FromDateTime(currentDate),
                                    AllocatedHours = phase2HoursPerDay,
                                    AllocationType = "Development",
                                    CreatedDate = DateTime.UtcNow
                                };
                                _context.ProjectAllocations.Add(allocation);
                                Console.WriteLine($"[APPLY SCHEDULE] Phase 2: {currentDate:yyyy-MM-dd}: {phase2HoursPerDay:F2}h");
                            }
                            currentDate = currentDate.AddDays(1);
                        }

                        Console.WriteLine($"[APPLY SCHEDULE] Phase 2 complete: {phase2WorkingDays} days allocated");
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
                    TotalHours = 40,
                    OnsiteType = "UAT",
                    CreatedDate = DateTime.UtcNow
                };
                _context.OnsiteSchedules.Add(uatSchedule);

                // Create UAT ProjectAllocation records (8h per day for 5 days)
                for (int i = 0; i < 5; i++)
                {
                    var uatDate = uatWeekStart.AddDays(i);
                    var uatAllocation = new ProjectAllocation
                    {
                        ProjectId = projectId,
                        SquadId = squadId,
                        AllocationDate = DateOnly.FromDateTime(uatDate),
                        AllocatedHours = 8,
                        AllocationType = "UAT",
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.ProjectAllocations.Add(uatAllocation);
                    Console.WriteLine($"[APPLY SCHEDULE] UAT allocation: {uatDate:yyyy-MM-dd}: 8h");
                }

                // Create Go-Live onsite schedule (1 engineer for 1 week)
                var goLiveWeekStart = GetMondayOfWeek(suggestion.EstimatedGoLiveDate);
                var goLiveSchedule = new OnsiteSchedule
                {
                    ProjectId = projectId,
                    WeekStartDate = goLiveWeekStart,
                    EngineerCount = 1,
                    TotalHours = 40,
                    OnsiteType = "GoLive",
                    CreatedDate = DateTime.UtcNow
                };
                _context.OnsiteSchedules.Add(goLiveSchedule);

                // Create Go-Live ProjectAllocation records (8h per day for 5 days)
                for (int i = 0; i < 5; i++)
                {
                    var goLiveDate = goLiveWeekStart.AddDays(i);
                    var goLiveAllocation = new ProjectAllocation
                    {
                        ProjectId = projectId,
                        SquadId = squadId,
                        AllocationDate = DateOnly.FromDateTime(goLiveDate),
                        AllocatedHours = 8,
                        AllocationType = "GoLive",
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.ProjectAllocations.Add(goLiveAllocation);
                    Console.WriteLine($"[APPLY SCHEDULE] Go-Live allocation: {goLiveDate:yyyy-MM-dd}: 8h");
                }

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
            DateTime? endDate = null,
            decimal minHoursPerDay = 2m,  // Don't allocate less than 2 hours/day
            int maxDurationDays = 180)    // Don't stretch project beyond 180 working days
        {
            var currentDate = startDate;
            var hoursAllocated = 0m;
            var schedule = new AllocationSchedule { StartDate = startDate };
            var workingDaysUsed = 0;

            Console.WriteLine($"[FLEXIBLE SCHEDULE] Starting search from {startDate:yyyy-MM-dd} for {totalHours}h" + (endDate.HasValue ? $" ending by {endDate.Value:yyyy-MM-dd}" : ""));

            while (hoursAllocated < totalHours && workingDaysUsed < maxDurationDays)
            {
                // Skip weekends
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Check if we've exceeded the end date
                if (endDate.HasValue && currentDate > endDate.Value)
                {
                    Console.WriteLine($"[FLEXIBLE SCHEDULE] Exceeded end date {endDate.Value:yyyy-MM-dd}");
                    return null;
                }

                // Get remaining capacity for this day (allowing up to 120% capacity)
                var totalCapacity = await _capacityService.GetSquadDailyCapacity(squadId);
                var allocatedHours = await _capacityService.GetSquadAllocatedHours(squadId, currentDate);
                var maxAllowedCapacity = totalCapacity * 1.2m; // Allow 20% over-allocation
                var remainingCapacity = maxAllowedCapacity - allocatedHours;

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

        // Even allocation algorithm - distributes hours evenly across timeframe
        private async Task<AllocationSchedule?> TryFindEvenAllocationWindow(
            int squadId,
            DateTime startDate,
            decimal totalHours,
            decimal dailyCapacity,
            DateTime endDate)
        {
            Console.WriteLine($"[EVEN ALLOCATION] Distributing {totalHours}h evenly from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // Count working days between start and end
            var workingDays = GetWorkingDays(startDate, endDate);
            if (workingDays <= 0)
            {
                Console.WriteLine($"[EVEN ALLOCATION] No working days in range");
                return null;
            }

            // Calculate hours per day
            var hoursPerDay = totalHours / workingDays;
            Console.WriteLine($"[EVEN ALLOCATION] {workingDays} working days, {hoursPerDay:F2}h per day");

            var schedule = new AllocationSchedule { StartDate = startDate };
            var currentDate = startDate;
            var totalAllocated = 0m;

            while (currentDate <= endDate)
            {
                // Skip weekends
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Check if this allocation would exceed 120% capacity
                var totalCapacity = await _capacityService.GetSquadDailyCapacity(squadId);
                var allocatedHours = await _capacityService.GetSquadAllocatedHours(squadId, currentDate);
                var maxAllowedCapacity = totalCapacity * 1.2m; // Allow 20% over-allocation
                var availableCapacity = maxAllowedCapacity - allocatedHours;

                if (hoursPerDay > availableCapacity)
                {
                    Console.WriteLine($"[EVEN ALLOCATION] {currentDate:yyyy-MM-dd}: Need {hoursPerDay:F2}h but only {availableCapacity:F2}h available (would exceed 120% limit)");
                    return null; // Cannot allocate without exceeding 120% limit
                }

                schedule.DailyAllocations[currentDate] = hoursPerDay;
                totalAllocated += hoursPerDay;

                currentDate = currentDate.AddDays(1);
            }

            schedule.EndDate = endDate;
            schedule.TotalHoursAllocated = totalAllocated;
            Console.WriteLine($"[EVEN ALLOCATION] Successfully allocated {totalAllocated:F2}h evenly across {schedule.DailyAllocations.Count} days");
            return schedule;
        }

        // Delayed allocation algorithm - reverse greedy from UAT date backwards
        private async Task<AllocationSchedule?> TryFindDelayedAllocationWindow(
            int squadId,
            DateTime endDate,
            decimal totalHours,
            decimal dailyCapacity,
            decimal minHoursPerDay = 2m,
            int maxDurationDays = 180)
        {
            var currentDate = endDate;
            var hoursAllocated = 0m;
            var schedule = new AllocationSchedule { EndDate = endDate };
            var workingDaysUsed = 0;

            Console.WriteLine($"[DELAYED SCHEDULE] Starting BACKWARD search from {endDate:yyyy-MM-dd} for {totalHours}h");

            while (hoursAllocated < totalHours && workingDaysUsed < maxDurationDays)
            {
                // Skip weekends
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDate = currentDate.AddDays(-1);
                    continue;
                }

                // Get remaining capacity for this day (allowing up to 120% capacity)
                var totalCapacity = await _capacityService.GetSquadDailyCapacity(squadId);
                var allocatedHours = await _capacityService.GetSquadAllocatedHours(squadId, currentDate);
                var maxAllowedCapacity = totalCapacity * 1.2m; // Allow 20% over-allocation
                var remainingCapacity = maxAllowedCapacity - allocatedHours;

                // Calculate how much we want to allocate
                var hoursRemaining = totalHours - hoursAllocated;
                var idealHours = Math.Min(dailyCapacity, hoursRemaining);

                // Take whatever capacity is available (up to what we need)
                var hoursToAllocate = Math.Min(remainingCapacity, idealHours);

                Console.WriteLine($"[DELAYED SCHEDULE] {currentDate:yyyy-MM-dd}: Remaining capacity={remainingCapacity}h, Ideal={idealHours}h, Allocating={hoursToAllocate}h");

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
                    // Set the start date to the earliest date we allocated
                    schedule.StartDate = currentDate;
                    schedule.TotalHoursAllocated = hoursAllocated;
                    Console.WriteLine($"[DELAYED SCHEDULE] Successfully allocated {hoursAllocated}h over {workingDaysUsed} working days, starting {currentDate:yyyy-MM-dd}");
                    return schedule;
                }

                // Move backwards one day
                currentDate = currentDate.AddDays(-1);
            }

            // Couldn't allocate all hours within constraints
            Console.WriteLine($"[DELAYED SCHEDULE] Failed to allocate all hours. Allocated {hoursAllocated}/{totalHours}h over {workingDaysUsed} days");
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

        // Calculate date based on week-based offsets
        private DateTime GetMondayOfPreviousWeek(DateTime date)
        {
            var monday = GetMondayOfWeek(date);
            return monday.AddDays(-7);
        }

        private DateTime GetMondayOfNextWeek(DateTime date)
        {
            var monday = GetMondayOfWeek(date);
            return monday.AddDays(7);
        }

        public async Task<List<AlgorithmComparison>> CompareAlgorithms(
            int projectId,
            int squadId,
            decimal? bufferPercentage = null,
            DateTime? startDate = null)
        {
            var algorithms = new[] { "greedy", "strict", "delayed" };
            var comparisons = new List<AlgorithmComparison>();

            foreach (var algo in algorithms)
            {
                var suggestion = await GetScheduleSuggestion(projectId, squadId, bufferPercentage, algo, startDate);

                var comparison = new AlgorithmComparison
                {
                    AlgorithmType = algo,
                    AlgorithmName = algo == "greedy" ? "Greedy" : algo == "strict" ? "Strict" : "Delayed",
                    Description = algo == "greedy"
                        ? "Uses all available capacity - starts earliest, may have variable daily hours"
                        : algo == "strict"
                            ? "Distributes hours evenly - consistent daily allocation, balanced workload"
                            : "Starts as late as possible - minimizes schedule risk, maximizes flexibility",
                    CanAllocate = suggestion.CanAllocate,
                    Message = suggestion.Message,
                    SuggestedStartDate = suggestion.CanAllocate ? suggestion.SuggestedStartDate : null,
                    EstimatedCodeCompleteDate = suggestion.CanAllocate ? suggestion.EstimatedCodeCompleteDate : null,
                    EstimatedCrpDate = suggestion.CanAllocate ? suggestion.EstimatedCrpDate : null,
                    EstimatedUatDate = suggestion.CanAllocate ? suggestion.EstimatedUatDate : null,
                    EstimatedGoLiveDate = suggestion.CanAllocate ? suggestion.EstimatedGoLiveDate : null,
                    EstimatedDurationDays = suggestion.CanAllocate ? suggestion.EstimatedDurationDays : null,
                    BufferedDevHours = suggestion.CanAllocate ? suggestion.BufferedDevHours : null
                };

                // Add pros and cons based on results
                if (suggestion.CanAllocate)
                {
                    if (algo == "greedy")
                    {
                        comparison.Pros = "Earliest possible start date";
                        comparison.Cons = "Variable daily hours may impact team consistency";
                    }
                    else if (algo == "strict")
                    {
                        comparison.Pros = "Even workload distribution, predictable daily allocation";
                        comparison.Cons = "May not start as early as Greedy";
                    }
                    else // delayed
                    {
                        comparison.Pros = "Latest start = maximum schedule flexibility";
                        comparison.Cons = "Less buffer time if issues arise";
                    }
                }

                comparisons.Add(comparison);
            }

            return comparisons;
        }
    }
}