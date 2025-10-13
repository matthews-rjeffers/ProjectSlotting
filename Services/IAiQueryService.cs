namespace ProjectScheduler.Services;

public interface IAiQueryService
{
    Task<AiQueryResult> ExecuteNaturalLanguageQuery(string question);
}

public class AiQueryResult
{
    public bool Success { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? GeneratedSql { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
    public List<Dictionary<string, object>>? Results { get; set; }
    public int? ResultCount { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
