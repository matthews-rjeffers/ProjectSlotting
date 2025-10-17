using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services
{
    public class AllocationService : IAllocationService
    {
        private readonly ProjectSchedulerDbContext _context;
        private readonly ICapacityService _capacityService;

        public AllocationService(ProjectSchedulerDbContext context, ICapacityService capacityService)
        {
            _context = context;
            _capacityService = capacityService;
        }

        public async Task<bool> AllocateProjectToSquad(int projectId, int squadId, DateTime startDate, DateTime crpDate, decimal totalDevHours)
        {
            // Get project with onsite schedules
            var project = await _context.Projects
                .Include(p => p.OnsiteSchedules)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                return false;

            // Remove any existing allocations for this project
            var existingAllocations = await _context.ProjectAllocations
                .Where(pa => pa.ProjectId == projectId)
                .ToListAsync();
            _context.ProjectAllocations.RemoveRange(existingAllocations);
            await _context.SaveChangesAsync();

            var allocations = new List<ProjectAllocation>();

            // NEW LOGIC: 90/10 split
            // Phase 1: 90% of dev hours from Start → CodeComplete
            // Phase 2: 10% of dev hours from CRP → UAT

            var codeCompleteDate = project.CodeCompleteDate ?? crpDate;
            var uatDate = project.Uatdate ?? crpDate;

            var ninetyPercentHours = totalDevHours * 0.9m;
            var tenPercentHours = totalDevHours * 0.1m;

            // Phase 1: Allocate 90% from Start to CodeComplete
            var phase1WorkingDays = GetWorkingDays(startDate, codeCompleteDate);

            Console.WriteLine($"[ALLOCATION DEBUG] === PHASE 1: 90% DEV HOURS (Start → CodeComplete) ===");
            Console.WriteLine($"[ALLOCATION DEBUG] Project: {project.ProjectNumber}, Squad: {squadId}");
            Console.WriteLine($"[ALLOCATION DEBUG] Period: {startDate:yyyy-MM-dd} to {codeCompleteDate:yyyy-MM-dd}");
            Console.WriteLine($"[ALLOCATION DEBUG] Working days: {phase1WorkingDays.Count}");
            Console.WriteLine($"[ALLOCATION DEBUG] 90% of dev hours: {ninetyPercentHours}h");

            if (phase1WorkingDays.Count == 0)
            {
                Console.WriteLine($"[ALLOCATION DEBUG] FAILED: No working days in Phase 1");
                return false;
            }

            var phase1HoursPerDay = ninetyPercentHours / phase1WorkingDays.Count;
            Console.WriteLine($"[ALLOCATION DEBUG] Phase 1 hours per day: {phase1HoursPerDay}h");

            foreach (var day in phase1WorkingDays)
            {
                var remainingCapacity = await _capacityService.GetSquadRemainingCapacity(squadId, day);

                if (remainingCapacity < phase1HoursPerDay)
                {
                    Console.WriteLine($"[ALLOCATION DEBUG] FAILED: Not enough capacity on {day:yyyy-MM-dd} (need {phase1HoursPerDay}h, have {remainingCapacity}h)");
                    return false;
                }

                allocations.Add(new ProjectAllocation
                {
                    ProjectId = projectId,
                    SquadId = squadId,
                    AllocationDate = DateOnly.FromDateTime(day),
                    AllocatedHours = phase1HoursPerDay,
                    AllocationType = "Development",
                    CreatedDate = DateTime.UtcNow
                });
            }

            Console.WriteLine($"[ALLOCATION DEBUG] Phase 1 complete: {phase1WorkingDays.Count} days allocated");

            // Phase 2: Allocate 10% from CRP to UAT
            var phase2WorkingDays = GetWorkingDays(crpDate, uatDate);

            Console.WriteLine($"[ALLOCATION DEBUG] === PHASE 2: 10% DEV HOURS (CRP → UAT) ===");
            Console.WriteLine($"[ALLOCATION DEBUG] Period: {crpDate:yyyy-MM-dd} to {uatDate:yyyy-MM-dd}");
            Console.WriteLine($"[ALLOCATION DEBUG] Working days: {phase2WorkingDays.Count}");
            Console.WriteLine($"[ALLOCATION DEBUG] 10% of dev hours: {tenPercentHours}h");

            if (phase2WorkingDays.Count == 0)
            {
                Console.WriteLine($"[ALLOCATION DEBUG] FAILED: No working days in Phase 2");
                return false;
            }

            var phase2HoursPerDay = tenPercentHours / phase2WorkingDays.Count;
            Console.WriteLine($"[ALLOCATION DEBUG] Phase 2 hours per day: {phase2HoursPerDay}h");

            foreach (var day in phase2WorkingDays)
            {
                var dateOnly = DateOnly.FromDateTime(day);

                // Check capacity including Phase 1 allocations
                var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squadId);
                var dbAllocatedHours = await _context.ProjectAllocations
                    .Where(pa => pa.SquadId == squadId && pa.AllocationDate == dateOnly)
                    .SumAsync(pa => pa.AllocatedHours);
                var localAllocatedHours = allocations
                    .Where(a => a.AllocationDate == dateOnly)
                    .Sum(a => a.AllocatedHours);
                var allocatedHours = dbAllocatedHours + localAllocatedHours;
                var remainingCapacity = dailyCapacity - allocatedHours;

                if (remainingCapacity < phase2HoursPerDay)
                {
                    Console.WriteLine($"[ALLOCATION DEBUG] FAILED: Not enough capacity on {day:yyyy-MM-dd} (need {phase2HoursPerDay}h, have {remainingCapacity}h)");
                    return false;
                }

                allocations.Add(new ProjectAllocation
                {
                    ProjectId = projectId,
                    SquadId = squadId,
                    AllocationDate = dateOnly,
                    AllocatedHours = phase2HoursPerDay,
                    AllocationType = "Development",
                    CreatedDate = DateTime.UtcNow
                });
            }

            Console.WriteLine($"[ALLOCATION DEBUG] Phase 2 complete: {phase2WorkingDays.Count} days allocated");
            Console.WriteLine($"[ALLOCATION DEBUG] === PHASE 3: ONSITE HOURS ===");

            // Phase 2: Onsite Hours - Based on OnsiteSchedule entries
            // Each entry specifies a week, engineer count, and type (UAT or GoLive)
            foreach (var schedule in project.OnsiteSchedules)
            {
                var weekStart = schedule.WeekStartDate;
                var weekEnd = weekStart.AddDays(4); // Monday to Friday
                var workingDays = GetWorkingDays(weekStart, weekEnd);

                // Use TotalHours from the schedule (user-defined)
                var hoursPerDay = (decimal)schedule.TotalHours / workingDays.Count;

                Console.WriteLine($"[ALLOCATION DEBUG] Processing onsite schedule for week {weekStart:yyyy-MM-dd}");
                Console.WriteLine($"[ALLOCATION DEBUG] Engineer count: {schedule.EngineerCount}, Total hours: {schedule.TotalHours}h, Type: {schedule.OnsiteType}");
                Console.WriteLine($"[ALLOCATION DEBUG] Working days: {workingDays.Count}, Hours per day: {hoursPerDay}h");

                foreach (var day in workingDays)
                {
                    var dateOnly = DateOnly.FromDateTime(day);

                    // Check if we have capacity (onsite can overlap with dev work)
                    var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squadId);

                    // Include hours from database AND from local allocations list (dev phase)
                    var dbAllocatedHours = await _context.ProjectAllocations
                        .Where(pa => pa.SquadId == squadId && pa.AllocationDate == dateOnly)
                        .SumAsync(pa => pa.AllocatedHours);
                    var localAllocatedHours = allocations
                        .Where(a => a.AllocationDate == dateOnly)
                        .Sum(a => a.AllocatedHours);
                    var allocatedHours = dbAllocatedHours + localAllocatedHours;

                    var remainingCapacity = dailyCapacity - allocatedHours;

                    Console.WriteLine($"[ALLOCATION DEBUG] Date: {day:yyyy-MM-dd} ({day.DayOfWeek})");
                    Console.WriteLine($"[ALLOCATION DEBUG]   Daily Capacity: {dailyCapacity}h");
                    Console.WriteLine($"[ALLOCATION DEBUG]   Already Allocated: {allocatedHours}h");
                    Console.WriteLine($"[ALLOCATION DEBUG]   Remaining: {remainingCapacity}h");
                    Console.WriteLine($"[ALLOCATION DEBUG]   Needed: {hoursPerDay}h");

                    if (remainingCapacity < hoursPerDay)
                    {
                        Console.WriteLine($"[ALLOCATION DEBUG] FAILED: Not enough capacity on {day:yyyy-MM-dd}");
                        // Not enough capacity - rollback
                        return false;
                    }

                    // Add onsite allocation (can coexist with dev on same day)
                    allocations.Add(new ProjectAllocation
                    {
                        ProjectId = projectId,
                        SquadId = squadId,
                        AllocationDate = dateOnly,
                        AllocatedHours = hoursPerDay,
                        AllocationType = schedule.OnsiteType, // "UAT" or "GoLive"
                        CreatedDate = DateTime.UtcNow
                    });

                    Console.WriteLine($"[ALLOCATION DEBUG]   Added {hoursPerDay}h {schedule.OnsiteType} allocation");
                }
            }

            // Save all allocations
            await _context.ProjectAllocations.AddRangeAsync(allocations);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReallocateProject(int projectId, int squadId, DateTime newStartDate)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return false;

            if (!project.Crpdate.HasValue)
                return false;

            // Use the project's estimated dev hours for reallocation
            // The AllocateProjectToSquad method will handle both dev and onsite hours
            return await AllocateProjectToSquad(projectId, squadId, newStartDate, project.Crpdate.Value, project.EstimatedDevHours);
        }

        public async Task RemoveProjectAllocations(int projectId, int? squadId = null)
        {
            var query = _context.ProjectAllocations.Where(pa => pa.ProjectId == projectId);

            if (squadId.HasValue)
            {
                query = query.Where(pa => pa.SquadId == squadId.Value);
            }

            var allocations = await query.ToListAsync();
            _context.ProjectAllocations.RemoveRange(allocations);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProjectAllocation>> GetProjectAllocations(int projectId)
        {
            return await _context.ProjectAllocations
                .Where(pa => pa.ProjectId == projectId)
                .OrderBy(pa => pa.AllocationDate)
                .ToListAsync();
        }

        public async Task<bool> CanAllocateProject(int squadId, DateTime startDate, DateTime crpDate, decimal totalDevHours)
        {
            var workingDays = GetWorkingDays(startDate, crpDate);

            if (workingDays.Count == 0)
                return false;

            var hoursPerDay = totalDevHours / workingDays.Count;

            foreach (var day in workingDays)
            {
                var remainingCapacity = await _capacityService.GetSquadRemainingCapacity(squadId, day);
                if (remainingCapacity < hoursPerDay)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<AllocationPreview> PreviewProjectAllocation(int projectId, int squadId)
        {
            var preview = new AllocationPreview();

            // Get project with onsite schedules
            var project = await _context.Projects
                .Include(p => p.OnsiteSchedules)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                preview.CanAllocate = false;
                preview.Message = "Project not found";
                return preview;
            }

            if (!project.StartDate.HasValue)
            {
                preview.CanAllocate = false;
                preview.Message = "Project must have a start date";
                return preview;
            }

            if (!project.Crpdate.HasValue)
            {
                preview.CanAllocate = false;
                preview.Message = "Project must have a CRP date";
                return preview;
            }

            // Phase 1: Calculate dev hours (Start Date to UAT Date)
            var devEndDate = project.Uatdate ?? project.Crpdate.Value;
            var devWorkingDays = GetWorkingDays(project.StartDate.Value, devEndDate);
            var devHoursPerDay = devWorkingDays.Count > 0 ? project.EstimatedDevHours / devWorkingDays.Count : 0;

            // Get squad's daily capacity
            var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squadId);

            // Determine date range for preview
            var startDate = project.StartDate.Value;
            var endDate = project.GoLiveDate ?? devEndDate;

            // Get all onsite schedules to find the latest week
            if (project.OnsiteSchedules.Any())
            {
                var latestOnsiteWeek = project.OnsiteSchedules.Max(s => s.WeekStartDate.AddDays(4));
                if (latestOnsiteWeek > endDate)
                    endDate = latestOnsiteWeek;
            }

            // Get current allocations for the date range
            var currentAllocations = await _context.ProjectAllocations
                .Where(pa => pa.SquadId == squadId &&
                            pa.AllocationDate >= DateOnly.FromDateTime(startDate) &&
                            pa.AllocationDate <= DateOnly.FromDateTime(endDate))
                .ToListAsync();

            // Group by week
            var weeklyData = new Dictionary<DateTime, WeeklyCapacityPreview>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                var weekStart = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);

                if (!weeklyData.ContainsKey(weekStart))
                {
                    weeklyData[weekStart] = new WeeklyCapacityPreview
                    {
                        WeekStart = weekStart,
                        WeekEnd = weekStart.AddDays(4),
                        TotalCapacity = 0,
                        CurrentDevHours = 0,
                        CurrentOnsiteHours = 0,
                        PreviewDevHours = 0,
                        PreviewOnsiteHours = 0
                    };
                }

                var week = weeklyData[weekStart];
                week.TotalCapacity += dailyCapacity;

                // Current allocations (all types)
                var dateOnly = DateOnly.FromDateTime(date);
                var currentDayAllocations = currentAllocations.Where(a => a.AllocationDate == dateOnly);
                week.CurrentDevHours += currentDayAllocations.Where(a => a.AllocationType == "Development").Sum(a => a.AllocatedHours);
                week.CurrentOnsiteHours += currentDayAllocations.Where(a => a.AllocationType == "UAT" || a.AllocationType == "GoLive" || a.AllocationType == "Onsite").Sum(a => a.AllocatedHours);

                // Preview dev allocations
                if (devWorkingDays.Contains(date))
                    week.PreviewDevHours += devHoursPerDay;
            }

            // Phase 2: Add onsite schedule previews
            foreach (var schedule in project.OnsiteSchedules)
            {
                var weekStart = schedule.WeekStartDate;
                if (weekStart.DayOfWeek != DayOfWeek.Monday)
                {
                    // Adjust to Monday
                    var dayOfWeek = (int)weekStart.DayOfWeek;
                    weekStart = weekStart.AddDays(dayOfWeek == 0 ? -6 : -(dayOfWeek - 1));
                }

                if (!weeklyData.ContainsKey(weekStart))
                    continue; // Week not in range

                var weekEnd = weekStart.AddDays(4);
                var workingDays = GetWorkingDays(weekStart, weekEnd);
                var hoursPerDay = (decimal)schedule.TotalHours / workingDays.Count;

                weeklyData[weekStart].PreviewOnsiteHours += schedule.TotalHours;
            }

            // Calculate utilization and check capacity
            bool canAllocate = true;
            foreach (var week in weeklyData.Values)
            {
                var currentTotal = week.CurrentDevHours + week.CurrentOnsiteHours;
                var previewTotal = currentTotal + week.PreviewDevHours + week.PreviewOnsiteHours;

                week.CurrentUtilization = week.TotalCapacity > 0 ? (currentTotal / week.TotalCapacity) * 100 : 0;
                week.PreviewUtilization = week.TotalCapacity > 0 ? (previewTotal / week.TotalCapacity) * 100 : 0;
                week.WouldExceedCapacity = previewTotal > week.TotalCapacity;

                if (week.WouldExceedCapacity)
                    canAllocate = false;
            }

            preview.CanAllocate = canAllocate;
            preview.Message = canAllocate ? "Project can be allocated" : "Allocation would exceed squad capacity";
            preview.WeeklyCapacity = weeklyData.Values.OrderBy(w => w.WeekStart).ToList();

            return preview;
        }

        private List<DateTime> GetWorkingDays(DateTime startDate, DateTime endDate)
        {
            var workingDays = new List<DateTime>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // Skip weekends
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays.Add(date);
                }
            }

            return workingDays;
        }
    }
}
