using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;
using ProjectScheduler.Services;

namespace ProjectScheduler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SquadsController : ControllerBase
    {
        private readonly ProjectSchedulerDbContext _context;
        private readonly ICapacityService _capacityService;

        public SquadsController(ProjectSchedulerDbContext context, ICapacityService capacityService)
        {
            _context = context;
            _capacityService = capacityService;
        }

        // GET: api/Squads
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Squad>>> GetSquads()
        {
            return await _context.Squads
                .Include(s => s.TeamMembers)
                .Where(s => s.IsActive)
                .ToListAsync();
        }

        // GET: api/Squads/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Squad>> GetSquad(int id)
        {
            var squad = await _context.Squads
                .Include(s => s.TeamMembers)
                .FirstOrDefaultAsync(s => s.SquadId == id);

            if (squad == null)
            {
                return NotFound();
            }

            return squad;
        }

        // GET: api/Squads/5/capacity?startDate=2025-01-01&endDate=2025-12-31
        [HttpGet("{id}/capacity")]
        public async Task<ActionResult<Dictionary<DateTime, CapacityInfo>>> GetSquadCapacity(
            int id,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var squad = await _context.Squads.FindAsync(id);
            if (squad == null)
            {
                return NotFound();
            }

            var capacity = await _capacityService.GetSquadCapacityRange(id, startDate, endDate);
            return Ok(capacity);
        }

        // PUT: api/Squads/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSquad(int id, Squad squad)
        {
            if (id != squad.SquadId)
            {
                return BadRequest();
            }

            _context.Entry(squad).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SquadExists(id))
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

        // POST: api/Squads
        [HttpPost]
        public async Task<ActionResult<Squad>> PostSquad(Squad squad)
        {
            _context.Squads.Add(squad);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSquad), new { id = squad.SquadId }, squad);
        }

        // DELETE: api/Squads/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSquad(int id)
        {
            var squad = await _context.Squads.FindAsync(id);
            if (squad == null)
            {
                return NotFound();
            }

            // Check if squad has any allocations
            var hasAllocations = await _context.ProjectAllocations.AnyAsync(pa => pa.SquadId == id);
            if (hasAllocations)
            {
                return BadRequest(new { message = "Cannot delete squad with project allocations. Remove allocations first." });
            }

            // Soft delete
            squad.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SquadExists(int id)
        {
            return _context.Squads.Any(e => e.SquadId == id);
        }
    }
}
