using System.Data;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using OpenAI.Chat;
using ProjectScheduler.Utils;

namespace ProjectScheduler.Services;

public class AiQueryService : IAiQueryService
{
    private readonly string _connectionString;
    private readonly ChatClient _chatClient;
    private readonly ILogger<AiQueryService> _logger;
    private readonly string _schemaContext;

    public AiQueryService(
        IConfiguration configuration,
        ILogger<AiQueryService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found");

        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");

        var model = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";

        _chatClient = new ChatClient(model, apiKey);
        _logger = logger;

        // Load the RAG context (database schema)
        _schemaContext = LoadSchemaContext();
    }

    private string LoadSchemaContext()
    {
        try
        {
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "docs", "rag-context", "database-schema.md");

            if (File.Exists(schemaPath))
            {
                return File.ReadAllText(schemaPath);
            }

            _logger.LogWarning("Schema context file not found at {Path}", schemaPath);
            return GetFallbackSchema();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading schema context");
            return GetFallbackSchema();
        }
    }

    private string GetFallbackSchema()
    {
        return @"
Database Tables:
- Squads (SquadId, SquadName, SquadLeadName, IsActive)
- TeamMembers (TeamMemberId, SquadId, MemberName, Role, DailyCapacityHours, IsActive)
- Projects (ProjectId, ProjectNumber, CustomerName, CustomerCity, CustomerState, EstimatedDevHours, GoLiveDate, StartDate)
- ProjectAllocations (AllocationId, ProjectId, SquadId, AllocationDate, AllocatedHours, AllocationType)
- OnsiteSchedules (OnsiteScheduleId, ProjectId, WeekStartDate, EngineerCount, OnsiteType)
";
    }

    public async Task<AiQueryResult> ExecuteNaturalLanguageQuery(string question)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new AiQueryResult
        {
            Question = question,
            Success = false
        };

        try
        {
            // Step 1: Generate SQL using OpenAI
            _logger.LogInformation("Generating SQL for question: {Question}", question);
            var sql = await GenerateSqlFromQuestion(question);

            if (string.IsNullOrWhiteSpace(sql))
            {
                result.Error = "Unable to generate SQL query from your question. Please try rephrasing.";
                result.ErrorCode = "SQL_GENERATION_FAILED";
                return result;
            }

            result.GeneratedSql = sql;
            _logger.LogInformation("Generated SQL: {SQL}", sql);

            // Step 2: Validate SQL for safety
            var (isValid, validationError) = SqlValidator.ValidateQuery(sql);
            if (!isValid)
            {
                _logger.LogWarning("SQL validation failed: {Error}", validationError);
                result.Error = "I can only answer questions about your data, not modify it. Please rephrase your question.";
                result.ErrorCode = "UNSAFE_QUERY";
                return result;
            }

            // Step 3: Execute SQL query
            _logger.LogInformation("Executing SQL query");
            var queryResults = await ExecuteSqlQuery(sql);

            result.Results = queryResults;
            result.ResultCount = queryResults.Count;
            result.Success = true;

            _logger.LogInformation("Query executed successfully. Returned {Count} rows", queryResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing natural language query");
            result.Error = "An error occurred while processing your query. Please try again.";
            result.ErrorCode = "EXECUTION_ERROR";
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private async Task<string> GenerateSqlFromQuestion(string question)
    {
        var systemPrompt = $@"You are an expert Microsoft SQL Server query generator for a project scheduling database.

{_schemaContext}

Your task is to convert natural language questions into valid Microsoft SQL Server SELECT queries.

CRITICAL SYNTAX REQUIREMENTS (SQL Server):
1. Use TOP N instead of LIMIT N (e.g., SELECT TOP 5, not LIMIT 5)
2. Use GETDATE() for current date (not NOW() or CURRENT_DATE)
3. Use DATEADD(interval, number, date) for date arithmetic
4. ALWAYS use table aliases when joining tables (e.g., s.SquadId, pa.ProjectId)
5. When using GROUP BY, you MUST include all non-aggregated columns in the GROUP BY clause
6. When using GROUP BY with columns from joined tables, qualify ALL column names with table aliases

QUERY RULES:
1. Generate ONLY SELECT queries - no INSERT, UPDATE, DELETE operations
2. Return ONLY the SQL query - no explanations, markdown, or formatting
3. No semicolons, comments, or multiple statements
4. Always qualify column names with table aliases in JOINs
5. Always filter active records (IsActive = 1) unless asked for inactive ones
6. Use date ranges when querying ProjectAllocations for performance

EXAMPLES:

Question: Show all active squads
SQL: SELECT SquadId, SquadName, SquadLeadName FROM Squads WHERE IsActive = 1

Question: Which squads have projects in November 2024?
SQL: SELECT DISTINCT s.SquadName FROM Squads s JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId WHERE MONTH(pa.AllocationDate) = 11 AND YEAR(pa.AllocationDate) = 2024

Question: How much work is assigned to each squad?
SQL: SELECT s.SquadId, s.SquadName, SUM(pa.AllocatedHours) as TotalHours FROM Squads s LEFT JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId WHERE s.IsActive = 1 GROUP BY s.SquadId, s.SquadName

Question: Which squad has the most work assigned?
SQL: SELECT TOP 1 s.SquadName, SUM(pa.AllocatedHours) as TotalHours FROM Squads s JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId WHERE s.IsActive = 1 GROUP BY s.SquadId, s.SquadName ORDER BY TotalHours DESC

Question: Which squads have the most capacity in two weeks?
SQL: SELECT s.SquadId, s.SquadName, (capacity.TotalCapacity * 10) - ISNULL(allocated.TotalAllocated, 0) as RemainingCapacity FROM Squads s LEFT JOIN (SELECT SquadId, SUM(DailyCapacityHours) as TotalCapacity FROM TeamMembers WHERE IsActive = 1 GROUP BY SquadId) capacity ON s.SquadId = capacity.SquadId LEFT JOIN (SELECT SquadId, SUM(AllocatedHours) as TotalAllocated FROM ProjectAllocations WHERE AllocationDate BETWEEN DATEADD(WEEK, 2, CAST(GETDATE() AS DATE)) AND DATEADD(DAY, 9, DATEADD(WEEK, 2, CAST(GETDATE() AS DATE))) GROUP BY SquadId) allocated ON s.SquadId = allocated.SquadId WHERE s.IsActive = 1 ORDER BY RemainingCapacity DESC

Question: How much work is assigned in the next two weeks?
SQL: SELECT SUM(AllocatedHours) as TotalHours FROM ProjectAllocations WHERE AllocationDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 13, CAST(GETDATE() AS DATE))

Question: Which projects will cause squads to be overallocated and on what dates?
SQL: SELECT pa.AllocationDate, s.SquadName, p.ProjectNumber, p.CustomerName, capacity.DailyCapacity, SUM(pa.AllocatedHours) as TotalAllocated FROM ProjectAllocations pa JOIN Projects p ON pa.ProjectId = p.ProjectId JOIN Squads s ON pa.SquadId = s.SquadId JOIN (SELECT SquadId, SUM(DailyCapacityHours) as DailyCapacity FROM TeamMembers WHERE IsActive = 1 GROUP BY SquadId) capacity ON s.SquadId = capacity.SquadId WHERE s.IsActive = 1 GROUP BY pa.AllocationDate, s.SquadName, p.ProjectNumber, p.CustomerName, capacity.DailyCapacity HAVING SUM(pa.AllocatedHours) > capacity.DailyCapacity ORDER BY pa.AllocationDate, s.SquadName

Question: Which projects have onsite work scheduled in the next 2 months?
SQL: SELECT p.ProjectNumber, p.CustomerName, os.OnsiteType, os.WeekStartDate, os.EngineerCount FROM Projects p JOIN OnsiteSchedules os ON p.ProjectId = os.ProjectId WHERE os.WeekStartDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(MONTH, 2, CAST(GETDATE() AS DATE)) ORDER BY os.WeekStartDate

Question: Which squads have onsite work in the next 4 months and how many hours?
SQL: SELECT s.SquadName, os.OnsiteType, os.WeekStartDate, SUM(pa.AllocatedHours) as OnsiteHours FROM OnsiteSchedules os JOIN Projects p ON os.ProjectId = p.ProjectId JOIN ProjectAllocations pa ON p.ProjectId = pa.ProjectId AND pa.AllocationDate >= os.WeekStartDate AND pa.AllocationDate < DATEADD(WEEK, 1, os.WeekStartDate) JOIN Squads s ON pa.SquadId = s.SquadId WHERE os.WeekStartDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(MONTH, 4, CAST(GETDATE() AS DATE)) AND s.IsActive = 1 GROUP BY s.SquadName, os.OnsiteType, os.WeekStartDate ORDER BY s.SquadName, os.WeekStartDate

IMPORTANT: ONSITE VS ALLOCATIONS
- ""onsite work"" or ""onsite schedule"" = Use OnsiteSchedules table (tracks UAT/Go-Live events)
- ""allocated hours"" or ""work assigned"" = Use ProjectAllocations table (daily hour assignments)
- OnsiteSchedules links to Projects only, NOT Squads
- To find squads with onsite work: JOIN OnsiteSchedules → Projects → ProjectAllocations → Squads

IMPORTANT: CAPACITY ALWAYS MEANS REMAINING/AVAILABLE CAPACITY
- When asked about ""capacity"" for a future date or date range, you MUST calculate REMAINING capacity
- REMAINING capacity = (Total team capacity for period) - (Allocated hours in that period)
- NEVER just return total capacity - you must always subtract what's already allocated
- Example: ""capacity in two weeks"" means show remaining hours after subtracting allocations in that time period

IMPORTANT DATE HANDLING:
- ""in two weeks"" / ""two weeks from now"" = Starting date is DATEADD(WEEK, 2, CAST(GETDATE() AS DATE)), ending 10 working days later
- ""next two weeks"" / ""over the next two weeks"" = From today to 13 days from now: BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 13, CAST(GETDATE() AS DATE))
- ""this week"" = Current Monday to Friday (5 working days, 10 days for 2-week span)
- When calculating remaining capacity for a date range, you MUST:
  1. Calculate total squad capacity for that period
  2. JOIN with ProjectAllocations filtered by that date range
  3. Subtract allocated hours from total capacity
- Always use CAST(GETDATE() AS DATE) to get just the date without time

CRITICAL: AVOID CARTESIAN PRODUCTS
- NEVER do multiple LEFT JOINs that both aggregate (SUM, COUNT, etc.) - this causes incorrect multiplication of values
- Instead, use subqueries to pre-aggregate data before joining
- Example: JOIN (SELECT SquadId, SUM(DailyCapacityHours) as Total FROM TeamMembers GROUP BY SquadId) capacity ON s.SquadId = capacity.SquadId

CRITICAL: LEFT JOIN and WHERE Clause
- When using LEFT JOIN, NEVER put filter conditions for the RIGHT table in the WHERE clause
- This converts LEFT JOIN to INNER JOIN and excludes rows with NULL values
- WRONG: LEFT JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId WHERE pa.AllocationDate BETWEEN ...
- CORRECT: LEFT JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId AND pa.AllocationDate BETWEEN ...
- OR use subquery: LEFT JOIN (SELECT SquadId, SUM(...) FROM ProjectAllocations WHERE AllocationDate BETWEEN ... GROUP BY SquadId) pa ON s.SquadId = pa.SquadId
- If asked to show ""all squads"" or ""each squad"", you MUST use LEFT JOIN and include squads with zero values

Now generate SQL for the following question. Return ONLY the SQL query:";

        try
        {
            List<ChatMessage> messages = new()
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(question)
            };

            var completion = await _chatClient.CompleteChatAsync(messages);
            var sqlResponse = completion.Value.Content[0].Text.Trim();

            // Clean up the response (remove markdown if present)
            sqlResponse = CleanSqlResponse(sqlResponse);

            // Fix common SQL generation errors
            sqlResponse = FixCommonSqlErrors(sqlResponse);

            return sqlResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }

    private string CleanSqlResponse(string sql)
    {
        // Remove markdown code blocks if present
        sql = sql.Replace("```sql", "").Replace("```", "").Trim();

        // Remove any leading/trailing quotes
        sql = sql.Trim('"', '\'');

        // Sanitize the SQL
        sql = SqlValidator.SanitizeQuery(sql);

        return sql;
    }

    private string FixCommonSqlErrors(string sql)
    {
        // Fix MySQL LIMIT syntax to SQL Server TOP syntax
        sql = System.Text.RegularExpressions.Regex.Replace(
            sql,
            @"LIMIT\s+(\d+)",
            "TOP $1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Log if the query contains dangerous patterns that cause Cartesian products
        if (sql.Contains("LEFT JOIN TeamMembers", StringComparison.OrdinalIgnoreCase) &&
            sql.Contains("LEFT JOIN ProjectAllocations", StringComparison.OrdinalIgnoreCase) &&
            sql.Contains("SUM(", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Detected potential Cartesian product query - AI may have generated incorrect SQL");
            // Note: We log but don't auto-fix this as it's too complex to fix safely
        }

        return sql;
    }

    private async Task<List<Dictionary<string, object>>> ExecuteSqlQuery(string sql)
    {
        var results = new List<Dictionary<string, object>>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 30; // 30 second timeout

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                // Convert DateOnly to DateTime string for JSON serialization
                if (value is DateOnly dateOnly)
                {
                    value = dateOnly.ToString("yyyy-MM-dd");
                }
                // Convert DateTime to ISO string
                else if (value is DateTime dateTime)
                {
                    value = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");
                }

                row[columnName] = value!;
            }

            results.Add(row);
        }

        return results;
    }
}
