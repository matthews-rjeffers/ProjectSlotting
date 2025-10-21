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
        private readonly ISquadRecommendationService _squadRecommendationService;
        private readonly IConflictDetectionService _conflictDetectionService;

        public ProjectsController(
            ProjectSchedulerDbContext context,
            IAllocationService allocationService,
            IScheduleSuggestionService scheduleSuggestionService,
            ISquadRecommendationService squadRecommendationService,
            IConflictDetectionService conflictDetectionService)
        {
            _context = context;
            _allocationService = allocationService;
            _scheduleSuggestionService = scheduleSuggestionService;
            _squadRecommendationService = squadRecommendationService;
            _conflictDetectionService = conflictDetectionService;
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
            // Validate date order
            var validationError = ValidateProjectDates(project);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
            }

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

            // Check if project is allocated
            var isAllocated = await _context.ProjectAllocations
                .AnyAsync(pa => pa.ProjectId == id);

            if (isAllocated)
            {
                // Get the existing project to compare dates
                var existingProject = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == id);
                if (existingProject == null)
                {
                    return NotFound();
                }

                // Prevent changing any dates if project is allocated
                if (project.StartDate != existingProject.StartDate ||
                    project.CodeCompleteDate != existingProject.CodeCompleteDate ||
                    project.Crpdate != existingProject.Crpdate ||
                    project.Uatdate != existingProject.Uatdate ||
                    project.GoLiveDate != existingProject.GoLiveDate)
                {
                    return BadRequest(new { message = "Cannot modify project dates while project is allocated. Unassign the project first to change dates." });
                }
            }

            // Validate date order
            var validationError = ValidateProjectDates(project);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
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

        // GET: api/Projects/5/squad-recommendations
        [HttpGet("{projectId}/squad-recommendations")]
        public async Task<ActionResult<List<SquadRecommendation>>> GetSquadRecommendations(
            int projectId,
            [FromQuery] decimal? bufferPercentage = null,
            [FromQuery] string? algorithmType = null,
            [FromQuery] DateTime? startDate = null)
        {
            var recommendations = await _squadRecommendationService.GetSquadRecommendations(
                projectId,
                bufferPercentage,
                algorithmType,
                startDate);
            return Ok(recommendations);
        }

        // GET: api/Projects/5/algorithm-comparison/3
        [HttpGet("{projectId}/algorithm-comparison/{squadId}")]
        public async Task<ActionResult<List<AlgorithmComparison>>> CompareAlgorithms(
            int projectId,
            int squadId,
            [FromQuery] decimal? bufferPercentage = null,
            [FromQuery] DateTime? startDate = null)
        {
            var comparisons = await _scheduleSuggestionService.CompareAlgorithms(
                projectId,
                squadId,
                bufferPercentage,
                startDate);
            return Ok(comparisons);
        }

        // POST: api/Projects/5/check-conflicts
        [HttpPost("{projectId}/check-conflicts")]
        public async Task<ActionResult<ConflictCheckResult>> CheckConflicts(
            int projectId,
            [FromBody] ConflictCheckRequest request)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            ConflictCheckResult result;

            if (request.CheckScheduleSuggestion && request.Suggestion != null)
            {
                // Check conflicts for a schedule suggestion
                result = await _conflictDetectionService.CheckScheduleSuggestionConflicts(
                    projectId,
                    request.SquadId,
                    request.Suggestion);
            }
            else
            {
                // Check conflicts for a simple allocation
                var startDate = request.StartDate ?? DateTime.Now;
                var endDate = request.EndDate ?? (project.Crpdate.HasValue ? project.Crpdate.Value : startDate.AddDays(30));
                var estimatedHours = request.EstimatedHours ?? project.EstimatedDevHours;

                result = await _conflictDetectionService.CheckAllocationConflicts(
                    projectId,
                    request.SquadId,
                    startDate,
                    endDate,
                    estimatedHours);
            }

            return Ok(result);
        }

        // GET: api/Projects/gantt-data
        [HttpGet("gantt-data")]
        public async Task<ActionResult<GanttDataResponse>> GetGanttData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? squadId = null)
        {
            // Default to 3 months if no date range specified
            var rangeStart = startDate ?? DateTime.Today;
            var rangeEnd = endDate ?? rangeStart.AddMonths(3);

            // Get all squads with active allocations
            var squadsQuery = _context.Squads
                .Include(s => s.TeamMembers)
                .Where(s => s.IsActive);

            if (squadId.HasValue)
            {
                squadsQuery = squadsQuery.Where(s => s.SquadId == squadId.Value);
            }

            var squads = await squadsQuery.ToListAsync();

            var ganttSquads = new List<GanttSquad>();
            DateTime? overallMinDate = null;
            DateTime? overallMaxDate = null;

            foreach (var squad in squads)
            {
                // Get projects allocated to this squad within the date range
                var projectIds = await _context.ProjectAllocations
                    .Where(pa => pa.SquadId == squad.SquadId)
                    .Select(pa => pa.ProjectId)
                    .Distinct()
                    .ToListAsync();

                if (!projectIds.Any())
                {
                    continue; // Skip squads with no projects
                }

                var projects = await _context.Projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .Include(p => p.ProjectAllocations)
                    .Include(p => p.OnsiteSchedules)
                    .ToListAsync();

                var ganttProjects = new List<GanttProject>();

                foreach (var project in projects)
                {
                    // Get development phases (Development allocations)
                    // We need to split into TWO phases:
                    // Phase 1: Start → Code Complete (bulk dev, ~90%)
                    // Phase 2: CRP → UAT (polish dev, ~10%, max 40h)
                    var devAllocations = project.ProjectAllocations
                        .Where(pa => pa.SquadId == squad.SquadId && pa.AllocationType == "Development")
                        .ToList();

                    DevelopmentPhase? devPhase = null;
                    DevelopmentPhase? polishPhase = null;

                    if (devAllocations.Any())
                    {
                        var codeCompleteDate = project.CodeCompleteDate ?? project.Crpdate;

                        if (codeCompleteDate.HasValue)
                        {
                            // Split allocations into Phase 1 (before/on Code Complete) and Phase 2 (after CRP)
                            var phase1Allocations = devAllocations
                                .Where(a => a.AllocationDate.ToDateTime(TimeOnly.MinValue) <= codeCompleteDate.Value)
                                .ToList();

                            var phase2Allocations = project.Crpdate.HasValue
                                ? devAllocations
                                    .Where(a => a.AllocationDate.ToDateTime(TimeOnly.MinValue) >= project.Crpdate.Value)
                                    .ToList()
                                : new List<ProjectAllocation>();

                            // Create Phase 1: Start → Code Complete
                            if (phase1Allocations.Any())
                            {
                                devPhase = new DevelopmentPhase
                                {
                                    StartDate = phase1Allocations.Min(a => a.AllocationDate).ToDateTime(TimeOnly.MinValue),
                                    EndDate = codeCompleteDate.Value
                                };

                                if (!overallMinDate.HasValue || devPhase.StartDate < overallMinDate)
                                    overallMinDate = devPhase.StartDate;
                                if (!overallMaxDate.HasValue || devPhase.EndDate > overallMaxDate)
                                    overallMaxDate = devPhase.EndDate;
                            }

                            // Create Phase 2: CRP → UAT (polish phase)
                            if (phase2Allocations.Any() && project.Uatdate.HasValue)
                            {
                                polishPhase = new DevelopmentPhase
                                {
                                    StartDate = phase2Allocations.Min(a => a.AllocationDate).ToDateTime(TimeOnly.MinValue),
                                    EndDate = project.Uatdate.Value
                                };

                                if (!overallMinDate.HasValue || polishPhase.StartDate < overallMinDate)
                                    overallMinDate = polishPhase.StartDate;
                                if (!overallMaxDate.HasValue || polishPhase.EndDate > overallMaxDate)
                                    overallMaxDate = polishPhase.EndDate;
                            }
                        }
                        else
                        {
                            // Fallback: No dates set, show all dev as one phase
                            var devStart = devAllocations.Min(a => a.AllocationDate);
                            var devEnd = devAllocations.Max(a => a.AllocationDate);

                            devPhase = new DevelopmentPhase
                            {
                                StartDate = devStart.ToDateTime(TimeOnly.MinValue),
                                EndDate = devEnd.ToDateTime(TimeOnly.MinValue)
                            };

                            if (!overallMinDate.HasValue || devPhase.StartDate < overallMinDate)
                                overallMinDate = devPhase.StartDate;
                            if (!overallMaxDate.HasValue || devPhase.EndDate > overallMaxDate)
                                overallMaxDate = devPhase.EndDate;
                        }
                    }

                    // Get milestones
                    var milestones = new List<Milestone>();

                    // Add Code Complete milestone only if it differs from CRP
                    if (project.CodeCompleteDate.HasValue &&
                        project.Crpdate.HasValue &&
                        project.CodeCompleteDate.Value != project.Crpdate.Value)
                    {
                        milestones.Add(new Milestone { Type = "CodeComplete", Date = project.CodeCompleteDate.Value });
                        if (!overallMinDate.HasValue || project.CodeCompleteDate.Value < overallMinDate)
                            overallMinDate = project.CodeCompleteDate.Value;
                        if (!overallMaxDate.HasValue || project.CodeCompleteDate.Value > overallMaxDate)
                            overallMaxDate = project.CodeCompleteDate.Value;
                    }

                    if (project.Crpdate.HasValue)
                    {
                        milestones.Add(new Milestone { Type = "CRP", Date = project.Crpdate.Value });
                        if (!overallMinDate.HasValue || project.Crpdate.Value < overallMinDate)
                            overallMinDate = project.Crpdate.Value;
                        if (!overallMaxDate.HasValue || project.Crpdate.Value > overallMaxDate)
                            overallMaxDate = project.Crpdate.Value;
                    }
                    if (project.Uatdate.HasValue)
                    {
                        milestones.Add(new Milestone { Type = "UAT", Date = project.Uatdate.Value });
                        if (!overallMinDate.HasValue || project.Uatdate.Value < overallMinDate)
                            overallMinDate = project.Uatdate.Value;
                        if (!overallMaxDate.HasValue || project.Uatdate.Value > overallMaxDate)
                            overallMaxDate = project.Uatdate.Value;
                    }
                    if (project.GoLiveDate.HasValue)
                    {
                        milestones.Add(new Milestone { Type = "GoLive", Date = project.GoLiveDate.Value });
                        if (!overallMinDate.HasValue || project.GoLiveDate.Value < overallMinDate)
                            overallMinDate = project.GoLiveDate.Value;
                        if (!overallMaxDate.HasValue || project.GoLiveDate.Value > overallMaxDate)
                            overallMaxDate = project.GoLiveDate.Value;
                    }

                    // Get onsite phases
                    var onsitePhases = project.OnsiteSchedules
                        .Select(os => new OnsitePhase
                        {
                            Type = os.OnsiteType,
                            StartDate = os.StartDate,
                            EndDate = os.EndDate,
                            EngineerCount = os.EngineerCount,
                            TotalHours = os.TotalHours
                        })
                        .ToList();

                    // Create milestones for onsite schedules
                    foreach (var phase in onsitePhases)
                    {
                        // Add milestone for the start of each onsite period
                        milestones.Add(new Milestone
                        {
                            Type = phase.Type,
                            Date = phase.StartDate
                        });

                        if (!overallMinDate.HasValue || phase.StartDate < overallMinDate)
                            overallMinDate = phase.StartDate;
                        if (!overallMaxDate.HasValue || phase.EndDate > overallMaxDate)
                            overallMaxDate = phase.EndDate;
                    }

                    // Only include project if it has data in the visible range
                    if (devPhase != null || polishPhase != null || milestones.Any() || onsitePhases.Any())
                    {
                        ganttProjects.Add(new GanttProject
                        {
                            ProjectId = project.ProjectId,
                            ProjectNumber = project.ProjectNumber,
                            CustomerName = project.CustomerName,
                            DevelopmentPhase = devPhase,
                            PolishPhase = polishPhase,
                            Milestones = milestones,
                            OnsitePhases = onsitePhases
                        });
                    }
                }

                if (ganttProjects.Any())
                {
                    ganttSquads.Add(new GanttSquad
                    {
                        SquadId = squad.SquadId,
                        SquadName = squad.SquadName,
                        Projects = ganttProjects
                    });
                }
            }

            // Use the requested start date, but extend the end date to include all project data
            // Add 2 weeks of blank space after the last allocation
            var actualEndDate = overallMaxDate.HasValue && overallMaxDate.Value > rangeEnd
                ? overallMaxDate.Value.AddDays(14)
                : rangeEnd;

            return Ok(new GanttDataResponse
            {
                Squads = ganttSquads,
                DateRange = new DateRange
                {
                    MinDate = rangeStart,
                    MaxDate = actualEndDate
                }
            });
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }

        private string? ValidateProjectDates(Project project)
        {
            // Rule 1: Start < CRP < UAT < Go Live
            if (project.StartDate.HasValue && project.Crpdate.HasValue && project.StartDate >= project.Crpdate)
            {
                return "Start Date must be before CRP Date";
            }

            if (project.Crpdate.HasValue && project.Uatdate.HasValue && project.Crpdate >= project.Uatdate)
            {
                return "CRP Date must be before UAT Date";
            }

            if (project.Uatdate.HasValue && project.GoLiveDate.HasValue && project.Uatdate >= project.GoLiveDate)
            {
                return "UAT Date must be before Go-Live Date";
            }

            // Rule 2: Start < Code Complete < UAT
            if (project.StartDate.HasValue && project.CodeCompleteDate.HasValue && project.StartDate >= project.CodeCompleteDate)
            {
                return "Start Date must be before Code Complete Date";
            }

            if (project.CodeCompleteDate.HasValue && project.Uatdate.HasValue && project.CodeCompleteDate >= project.Uatdate)
            {
                return "Code Complete Date must be before UAT Date";
            }

            // Code Complete can be before OR after CRP, so no validation needed between them

            return null; // All validations passed
        }
    }

    public class ConflictCheckRequest
    {
        public int SquadId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public bool CheckScheduleSuggestion { get; set; }
        public ScheduleSuggestion? Suggestion { get; set; }
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
