using Microsoft.AspNetCore.Mvc;
using ProjectScheduler.Services;

namespace ProjectScheduler.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiQueryController : ControllerBase
{
    private readonly IAiQueryService _aiQueryService;
    private readonly ILogger<AiQueryController> _logger;

    public AiQueryController(
        IAiQueryService aiQueryService,
        ILogger<AiQueryController> logger)
    {
        _aiQueryService = aiQueryService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AiQueryResult>> Query([FromBody] AiQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Question cannot be empty" });
        }

        _logger.LogInformation("Received AI query: {Question}", request.Question);

        try
        {
            var result = await _aiQueryService.ExecuteNaturalLanguageQuery(request.Question);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI query");
            return StatusCode(500, new AiQueryResult
            {
                Success = false,
                Question = request.Question,
                Error = "An internal error occurred while processing your query.",
                ErrorCode = "INTERNAL_ERROR"
            });
        }
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            message = "AI Query API is running",
            timestamp = DateTime.UtcNow,
            examples = new[]
            {
                "Show all active squads",
                "Which squads have availability next week?",
                "List all projects going live this month",
                "What's the capacity for Squad Alpha?"
            }
        });
    }
}

public class AiQueryRequest
{
    public string Question { get; set; } = string.Empty;
}
