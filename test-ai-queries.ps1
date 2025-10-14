# Test script for AI query feature
# Tests challenging queries to see if they work

$baseUrl = "http://localhost:5000/api/aiquery"
$queries = @(
    "Which projects will cause squads to be overallocated and on what dates?",
    "What's the average time between when projects are created and when they start, grouped by squad?",
    "Show me squads that have gaps in their schedule in the next month",
    "Which team members are working on the most projects simultaneously right now?",
    "How many projects can Sohail's Squad take on in December without exceeding 80% utilization?",
    "Which squads have onsite work scheduled but don't have enough capacity to cover it?",
    "Show me the top 3 most overbooked weeks across all squads in 2025",
    "What percentage of total company capacity is allocated to projects with Go-Live dates in Q4 2024?",
    "Find squads where the squad lead has less daily capacity than at least one developer on their team"
)

Write-Host "Testing AI Query Feature - Challenging Questions" -ForegroundColor Cyan
Write-Host "=" * 60

$testNumber = 2  # Starting from #2 since #1 was already tested

foreach ($query in $queries) {
    Write-Host "`nTest #$testNumber`: $query" -ForegroundColor Yellow

    $body = @{
        question = $query
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri $baseUrl -Method Post -Body $body -ContentType "application/json"

        if ($response.success) {
            Write-Host "  ✓ SUCCESS" -ForegroundColor Green
            Write-Host "  SQL: $($response.generatedSql.Substring(0, [Math]::Min(100, $response.generatedSql.Length)))..." -ForegroundColor Gray
            Write-Host "  Rows returned: $($response.resultCount)" -ForegroundColor Gray
        } else {
            Write-Host "  ✗ FAILED" -ForegroundColor Red
            Write-Host "  Error: $($response.error)" -ForegroundColor Red
            Write-Host "  SQL: $($response.generatedSql)" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "  ✗ EXCEPTION" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }

    $testNumber++
    Start-Sleep -Milliseconds 500  # Brief pause between requests
}

Write-Host "`n" + "=" * 60
Write-Host "Testing complete!" -ForegroundColor Cyan
