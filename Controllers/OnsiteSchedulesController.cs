using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;
using ProjectScheduler.Services;
using System.ComponentModel.DataAnnotations;

namespace ProjectScheduler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnsiteSchedulesController : ControllerBase
    {
        private readonly ProjectSchedulerDbContext _context;
        private readonly ICapacityService _capacityService;

        public OnsiteSchedulesController(ProjectSchedulerDbContext context, ICapacityService capacityService)
        {
            _context = context;
            _capacityService = capacityService;
        }

        // GET: api/OnsiteSchedules/project/5
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<OnsiteSchedule>>> GetProjectOnsiteSchedules(int projectId)
        {
            return await _context.OnsiteSchedules
                .Where(os => os.ProjectId == projectId)
                .OrderBy(os => os.WeekStartDate)
                .ToListAsync();
        }

        // POST: api/OnsiteSchedules
        [HttpPost]
        public async Task<ActionResult<OnsiteSchedule>> CreateOnsiteSchedule(CreateOnsiteScheduleDto dto)
        {
            var schedule = new OnsiteSchedule
            {
                ProjectId = dto.ProjectId,
                WeekStartDate = dto.WeekStartDate,
                EngineerCount = dto.EngineerCount,
                TotalHours = dto.TotalHours,
                OnsiteType = dto.OnsiteType,
                Notes = dto.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.OnsiteSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Check if this project is already allocated to a squad
            var existingAllocation = await _context.ProjectAllocations
                .Where(pa => pa.ProjectId == dto.ProjectId)
                .FirstOrDefaultAsync();

            if (existingAllocation != null)
            {
                // Project is allocated - create allocations for this new onsite schedule
                var squadId = existingAllocation.SquadId;
                await CreateAllocationsForSchedule(schedule, squadId);
            }

            return CreatedAtAction(nameof(GetProjectOnsiteSchedules), new { projectId = schedule.ProjectId }, schedule);
        }

        private async Task CreateAllocationsForSchedule(OnsiteSchedule schedule, int squadId)
        {
            var weekStart = schedule.WeekStartDate;
            var weekEnd = weekStart.AddDays(4); // Monday to Friday
            var workingDays = GetWorkingDays(weekStart, weekEnd);
            var hoursPerDay = (decimal)schedule.TotalHours / workingDays.Count;

            foreach (var day in workingDays)
            {
                var dateOnly = DateOnly.FromDateTime(day);

                // Check capacity
                var dailyCapacity = await _capacityService.GetSquadDailyCapacity(squadId);
                var allocatedHours = await _context.ProjectAllocations
                    .Where(pa => pa.SquadId == squadId && pa.AllocationDate == dateOnly)
                    .SumAsync(pa => pa.AllocatedHours);

                if (dailyCapacity - allocatedHours >= hoursPerDay)
                {
                    // Add allocation
                    _context.ProjectAllocations.Add(new ProjectAllocation
                    {
                        ProjectId = schedule.ProjectId,
                        SquadId = squadId,
                        AllocationDate = dateOnly,
                        AllocatedHours = hoursPerDay,
                        AllocationType = schedule.OnsiteType,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private List<DateTime> GetWorkingDays(DateTime start, DateTime end)
        {
            var days = new List<DateTime>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    days.Add(date);
                }
            }
            return days;
        }

        // PUT: api/OnsiteSchedules/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOnsiteSchedule(int id, UpdateOnsiteScheduleDto dto)
        {
            if (id != dto.OnsiteScheduleId)
            {
                return BadRequest();
            }

            var schedule = await _context.OnsiteSchedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            // Check if project is allocated
            var existingAllocation = await _context.ProjectAllocations
                .Where(pa => pa.ProjectId == schedule.ProjectId)
                .FirstOrDefaultAsync();

            if (existingAllocation != null)
            {
                // Remove old allocations for this schedule's week
                var squadId = existingAllocation.SquadId;
                await RemoveAllocationsForSchedule(schedule, squadId);
            }

            // Update the properties
            schedule.ProjectId = dto.ProjectId;
            schedule.WeekStartDate = dto.WeekStartDate;
            schedule.EngineerCount = dto.EngineerCount;
            schedule.TotalHours = dto.TotalHours;
            schedule.OnsiteType = dto.OnsiteType;
            schedule.Notes = dto.Notes;
            schedule.UpdatedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OnsiteScheduleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Create new allocations with updated values
            if (existingAllocation != null)
            {
                await CreateAllocationsForSchedule(schedule, existingAllocation.SquadId);
            }

            return NoContent();
        }

        private async Task RemoveAllocationsForSchedule(OnsiteSchedule schedule, int squadId)
        {
            var weekStart = schedule.WeekStartDate;
            var weekEnd = weekStart.AddDays(4);
            var workingDays = GetWorkingDays(weekStart, weekEnd);

            foreach (var day in workingDays)
            {
                var dateOnly = DateOnly.FromDateTime(day);

                // Remove allocations for this project, squad, date, and onsite type
                var allocationsToRemove = await _context.ProjectAllocations
                    .Where(pa => pa.ProjectId == schedule.ProjectId
                        && pa.SquadId == squadId
                        && pa.AllocationDate == dateOnly
                        && pa.AllocationType == schedule.OnsiteType)
                    .ToListAsync();

                _context.ProjectAllocations.RemoveRange(allocationsToRemove);
            }

            await _context.SaveChangesAsync();
        }

        // DELETE: api/OnsiteSchedules/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOnsiteSchedule(int id)
        {
            var schedule = await _context.OnsiteSchedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            // Check if project is allocated and remove associated allocations
            var existingAllocation = await _context.ProjectAllocations
                .Where(pa => pa.ProjectId == schedule.ProjectId)
                .FirstOrDefaultAsync();

            if (existingAllocation != null)
            {
                await RemoveAllocationsForSchedule(schedule, existingAllocation.SquadId);
            }

            _context.OnsiteSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OnsiteScheduleExists(int id)
        {
            return _context.OnsiteSchedules.Any(e => e.OnsiteScheduleId == id);
        }
    }

    public class CreateOnsiteScheduleDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [Range(1, 20)]
        public int EngineerCount { get; set; }

        [Required]
        [Range(1, 200)]
        public int TotalHours { get; set; } = 40;

        [Required]
        [MaxLength(20)]
        public string OnsiteType { get; set; } = "UAT";

        public string? Notes { get; set; }
    }

    public class UpdateOnsiteScheduleDto
    {
        [Required]
        public int OnsiteScheduleId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [Range(1, 20)]
        public int EngineerCount { get; set; }

        [Required]
        [Range(1, 200)]
        public int TotalHours { get; set; } = 40;

        [Required]
        [MaxLength(20)]
        public string OnsiteType { get; set; } = "UAT";

        public string? Notes { get; set; }
    }
}
