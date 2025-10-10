using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;

namespace ProjectScheduler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamMembersController : ControllerBase
    {
        private readonly ProjectSchedulerDbContext _context;

        public TeamMembersController(ProjectSchedulerDbContext context)
        {
            _context = context;
        }

        // GET: api/TeamMembers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamMember>>> GetTeamMembers([FromQuery] int? squadId = null)
        {
            var query = _context.TeamMembers.AsQueryable();

            if (squadId.HasValue)
            {
                query = query.Where(tm => tm.SquadId == squadId.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/TeamMembers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TeamMember>> GetTeamMember(int id)
        {
            var teamMember = await _context.TeamMembers.FindAsync(id);

            if (teamMember == null)
            {
                return NotFound();
            }

            return teamMember;
        }

        // POST: api/TeamMembers
        [HttpPost]
        public async Task<ActionResult<TeamMember>> PostTeamMember(TeamMemberDto dto)
        {
            var teamMember = new TeamMember
            {
                SquadId = dto.SquadId,
                MemberName = dto.MemberName,
                Role = dto.Role,
                DailyCapacityHours = dto.DailyCapacityHours,
                IsActive = dto.IsActive
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeamMember), new { id = teamMember.TeamMemberId }, teamMember);
        }

        // PUT: api/TeamMembers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeamMember(int id, TeamMemberDto dto)
        {
            var teamMember = await _context.TeamMembers.FindAsync(id);
            if (teamMember == null)
            {
                return NotFound();
            }

            teamMember.SquadId = dto.SquadId;
            teamMember.MemberName = dto.MemberName;
            teamMember.Role = dto.Role;
            teamMember.DailyCapacityHours = dto.DailyCapacityHours;
            teamMember.IsActive = dto.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeamMemberExists(id))
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

        // DELETE: api/TeamMembers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeamMember(int id)
        {
            var teamMember = await _context.TeamMembers.FindAsync(id);
            if (teamMember == null)
            {
                return NotFound();
            }

            // Hard delete - permanently remove the team member
            _context.TeamMembers.Remove(teamMember);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeamMemberExists(int id)
        {
            return _context.TeamMembers.Any(e => e.TeamMemberId == id);
        }
    }

    public class TeamMemberDto
    {
        public int SquadId { get; set; }
        public required string MemberName { get; set; }
        public required string Role { get; set; }
        public decimal DailyCapacityHours { get; set; }
        public bool IsActive { get; set; }
    }
}
