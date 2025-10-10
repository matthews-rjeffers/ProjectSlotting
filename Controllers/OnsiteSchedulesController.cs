using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjectScheduler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnsiteSchedulesController : ControllerBase
    {
        private readonly ProjectScheduler.ProjectSchedulerDbContext _context;

        public OnsiteSchedulesController(ProjectScheduler.ProjectSchedulerDbContext context)
        {
            _context = context;
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
                OnsiteType = dto.OnsiteType,
                CreatedDate = DateTime.UtcNow
            };

            _context.OnsiteSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProjectOnsiteSchedules), new { projectId = schedule.ProjectId }, schedule);
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

            // Update the properties
            schedule.ProjectId = dto.ProjectId;
            schedule.WeekStartDate = dto.WeekStartDate;
            schedule.EngineerCount = dto.EngineerCount;
            schedule.OnsiteType = dto.OnsiteType;
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

            return NoContent();
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
        [MaxLength(20)]
        public string OnsiteType { get; set; } = "UAT";
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
        [MaxLength(20)]
        public string OnsiteType { get; set; } = "UAT";
    }
}
