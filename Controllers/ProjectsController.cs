using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Models;
using ProjectScheduler.Services;

namespace ProjectScheduler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectSchedulerDbContext _context;
        private readonly IAllocationService _allocationService;
        private readonly IScheduleSuggestionService _scheduleSuggestionService;

        public ProjectsController(
            ProjectSchedulerDbContext context,
            IAllocationService allocationService,
            IScheduleSuggestionService scheduleSuggestionService)
        {
            _context = context;
            _allocationService = allocationService;
            _scheduleSuggestionService = scheduleSuggestionService;
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects
                .Include(p => p.ProjectAllocations)
                .ToListAsync();
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectAllocations)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        // GET: api/Projects/5/allocations
        [HttpGet("{id}/allocations")]
        public async Task<ActionResult<IEnumerable<ProjectAllocation>>> GetProjectAllocations(int id)
        {
            var allocations = await _allocationService.GetProjectAllocations(id);
            return Ok(allocations);
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<ActionResult<Project>> PostProject(Project project)
        {
            project.CreatedDate = DateTime.UtcNow;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId }, project);
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, Project project)
        {
            if (id != project.ProjectId)
            {
                return BadRequest();
            }

            project.UpdatedDate = DateTime.UtcNow;
            _context.Entry(project).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(id))
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

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Remove allocations first
            await _allocationService.RemoveProjectAllocations(id);

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Projects/5/allocate
        [HttpPost("{id}/allocate")]
        public async Task<IActionResult> AllocateProject(int id, [FromBody] AllocationRequest request)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            if (!project.Crpdate.HasValue)
            {
                return BadRequest(new { message = "Project must have a CRP date to be allocated. Use Schedule Suggestion to set dates." });
            }

            var success = await _allocationService.AllocateProjectToSquad(
                id,
                request.SquadId,
                request.StartDate ?? project.StartDate ?? DateTime.Now,
                project.Crpdate.Value,
                project.EstimatedDevHours
            );

            if (!success)
            {
                return BadRequest(new { message = "Insufficient capacity to allocate project" });
            }

            return Ok(new { message = "Project allocated successfully" });
        }

        // POST: api/Projects/5/reallocate
        [HttpPost("{id}/reallocate")]
        public async Task<IActionResult> ReallocateProject(int id, [FromBody] AllocationRequest request)
        {
            var success = await _allocationService.ReallocateProject(
                id,
                request.SquadId,
                request.StartDate ?? DateTime.Now
            );

            if (!success)
            {
                return BadRequest(new { message = "Unable to reallocate project" });
            }

            return Ok(new { message = "Project reallocated successfully" });
        }

        // POST: api/Projects/can-allocate
        [HttpPost("can-allocate")]
        public async Task<ActionResult<bool>> CanAllocateProject([FromBody] CanAllocateRequest request)
        {
            var canAllocate = await _allocationService.CanAllocateProject(
                request.SquadId,
                request.StartDate,
                request.CRPDate,
                request.TotalDevHours
            );

            return Ok(new { canAllocate });
        }

        // POST: api/Projects/5/unassign
        [HttpPost("{id}/unassign")]
        public async Task<IActionResult> UnassignProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            await _allocationService.RemoveProjectAllocations(id);
            return Ok(new { message = "Project unassigned successfully" });
        }

        // GET: api/Projects/5/preview-allocation/3
        [HttpGet("{projectId}/preview-allocation/{squadId}")]
        public async Task<ActionResult<AllocationPreview>> PreviewProjectAllocation(int projectId, int squadId)
        {
            var preview = await _allocationService.PreviewProjectAllocation(projectId, squadId);
            return Ok(preview);
        }

        // GET: api/Projects/5/schedule-suggestion/3
        [HttpGet("{projectId}/schedule-suggestion/{squadId}")]
        public async Task<ActionResult<ScheduleSuggestion>> GetScheduleSuggestion(
            int projectId,
            int squadId,
            [FromQuery] decimal? bufferPercentage = null,
            [FromQuery] string? algorithmType = null,
            [FromQuery] DateTime? startDate = null)
        {
            var suggestion = await _scheduleSuggestionService.GetScheduleSuggestion(
                projectId,
                squadId,
                bufferPercentage,
                algorithmType,
                startDate);
            return Ok(suggestion);
        }

        // POST: api/Projects/5/apply-schedule-suggestion
        [HttpPost("{projectId}/apply-schedule-suggestion")]
        public async Task<IActionResult> ApplyScheduleSuggestion(int projectId, [FromBody] ApplyScheduleRequest request)
        {
            // First get the suggestion to verify it
            var suggestion = await _scheduleSuggestionService.GetScheduleSuggestion(
                projectId,
                request.SquadId,
                request.BufferPercentage,
                request.AlgorithmType,
                request.StartDate);

            if (!suggestion.CanAllocate)
            {
                return BadRequest(new { message = suggestion.Message });
            }

            var success = await _scheduleSuggestionService.ApplyScheduleSuggestion(projectId, request.SquadId, suggestion);

            if (!success)
            {
                return BadRequest(new { message = "Failed to apply schedule suggestion" });
            }

            return Ok(new { message = "Schedule applied successfully", suggestion });
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }
    }

    public class AllocationRequest
    {
        public int SquadId { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class CanAllocateRequest
    {
        public int SquadId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CRPDate { get; set; }
        public decimal TotalDevHours { get; set; }
    }

    public class ApplyScheduleRequest
    {
        public int SquadId { get; set; }
        public decimal? BufferPercentage { get; set; }
        public string? AlgorithmType { get; set; }
        public DateTime? StartDate { get; set; }
    }
}
