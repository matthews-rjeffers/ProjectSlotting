using System.Text.RegularExpressions;

namespace ProjectScheduler.Utils;

/// <summary>
/// Validates SQL queries to prevent SQL injection and ensure only safe SELECT queries are executed
/// </summary>
public static class SqlValidator
{
    private static readonly string[] DangerousKeywords = new[]
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE", "EXEC", "EXECUTE",
        "CREATE", "GRANT", "REVOKE", "DENY", "BACKUP", "RESTORE", "SHUTDOWN",
        "xp_", "sp_", "OPENROWSET", "OPENDATASOURCE", "BULK INSERT"
    };

    private static readonly string[] BlockedPatterns = new[]
    {
        @";",                           // Multiple statements
        @"--",                          // SQL comments
        @"/\*",                         // Block comments start
        @"\*/",                         // Block comments end
        @"UNION\s+ALL",                 // Union queries (injection risk)
        @"UNION\s+SELECT",              // Union queries (injection risk)
        @"INTO\s+OUTFILE",              // File operations
        @"INTO\s+DUMPFILE",             // File operations
        @"LOAD_FILE",                   // File operations
        @"@@version",                   // System variables
        @"@@servername",                // System variables
        @"sys\.",                       // System tables
        @"information_schema\.",        // System schemas
        @"master\.",                    // Master database
        @"msdb\.",                      // MSDB database
        @"tempdb\.",                    // TempDB database
    };

    private static readonly Regex SelectStatementRegex = new Regex(
        @"^\s*SELECT\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// Validates that the SQL query is safe to execute
    /// </summary>
    /// <param name="sql">The SQL query to validate</param>
    /// <returns>A tuple indicating if the query is valid and an error message if not</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return (false, "SQL query cannot be empty");
        }

        // Must start with SELECT
        if (!SelectStatementRegex.IsMatch(sql))
        {
            return (false, "Only SELECT queries are allowed");
        }

        // Check for dangerous keywords (whole words only, not substrings)
        foreach (var keyword in DangerousKeywords)
        {
            // Use word boundary regex to match whole words only
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            if (regex.IsMatch(sql))
            {
                return (false, $"Query contains blocked keyword: {keyword}");
            }
        }

        // Check for blocked patterns
        foreach (var pattern in BlockedPatterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            if (regex.IsMatch(sql))
            {
                return (false, $"Query contains blocked pattern");
            }
        }

        // Additional safety: Check for unusual characters that might indicate injection
        if (sql.Contains('\0') || sql.Contains('\x1a'))
        {
            return (false, "Query contains invalid characters");
        }

        return (true, null);
    }

    /// <summary>
    /// Sanitizes the SQL query by removing leading/trailing whitespace and normalizing
    /// </summary>
    public static string SanitizeQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        // Remove leading/trailing whitespace
        sql = sql.Trim();

        // Normalize line endings
        sql = sql.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // Remove multiple spaces
        sql = Regex.Replace(sql, @"\s+", " ");

        return sql;
    }
}
