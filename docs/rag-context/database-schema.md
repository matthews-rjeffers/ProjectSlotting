# Project Scheduler Database Schema

## Overview
This database manages squad capacity planning, team members, projects, and their allocations.

---

## Tables

### Squads
Represents development squads/teams.

**Columns:**
- `SquadId` (int, PK): Unique identifier for the squad
- `SquadName` (string): Name of the squad (e.g., "Alpha Squad", "Bravo Squad")
- `SquadLeadName` (string): Name of the squad leader
- `IsActive` (bit): Whether the squad is currently active (1 = active, 0 = inactive)

**Relationships:**
- Has many `TeamMembers` (via SquadId)
- Has many `ProjectAllocations` (via SquadId)

**Example Data:**
```sql
SquadId | SquadName    | SquadLeadName | IsActive
--------|--------------|---------------|----------
1       | Alpha Squad  | John Smith    | 1
2       | Bravo Squad  | Jane Doe      | 1
3       | Charlie Team | Bob Johnson   | 0
```

---

### TeamMembers
Individual members within squads.

**Columns:**
- `TeamMemberId` (int, PK): Unique identifier for the team member
- `SquadId` (int, FK): References Squads.SquadId
- `MemberName` (string): Full name of the team member
- `Role` (string): Job role (e.g., "Squad Lead", "Project Lead", "Developer")
- `DailyCapacityHours` (decimal): Hours available per working day (typically 4-8)
- `IsActive` (bit): Whether the member is currently active (1 = active, 0 = inactive)

**Relationships:**
- Belongs to one `Squad` (via SquadId)

**Example Data:**
```sql
TeamMemberId | SquadId | MemberName    | Role         | DailyCapacityHours | IsActive
-------------|---------|---------------|--------------|--------------------|---------
1            | 1       | John Smith    | Squad Lead   | 6.0                | 1
2            | 1       | Alice Cooper  | Developer    | 8.0                | 1
3            | 1       | Mark Taylor   | Developer    | 8.0                | 1
4            | 2       | Jane Doe      | Squad Lead   | 6.0                | 1
```

---

### Projects
Customer projects that need squad allocation.

**Columns:**
- `ProjectId` (int, PK): Unique identifier for the project
- `ProjectNumber` (string): Unique project code (e.g., "P-2024-001")
- `CustomerName` (string): Name of the customer
- `CustomerCity` (string): City where customer is located
- `CustomerState` (string): State where customer is located
- `EstimatedDevHours` (decimal): Estimated hours for development work
- `EstimatedOnsiteHours` (decimal): Estimated hours for onsite/UAT work
- `GoLiveDate` (datetime, nullable): Date project goes live in production
- `CRPDate` (datetime, nullable): Conference Room Pilot date (3 working days before UAT)
- `UATDate` (datetime, nullable): User Acceptance Testing date
- `BufferPercentage` (decimal): Buffer percentage for estimates (default 20%)
- `JiraLink` (string): Link to Jira ticket
- `StartDate` (datetime, nullable): When development starts
- `CreatedDate` (datetime): When project was created
- `UpdatedDate` (datetime, nullable): When project was last updated

**Relationships:**
- Has many `ProjectAllocations` (via ProjectId)
- Has many `OnsiteSchedules` (via ProjectId)

**Example Data:**
```sql
ProjectId | ProjectNumber | CustomerName    | EstimatedDevHours | GoLiveDate  | StartDate
----------|---------------|-----------------|-------------------|-------------|------------
1         | P-2024-001    | Acme Corp       | 80.0              | 2024-12-01  | 2024-11-01
2         | P-2024-002    | Widget Inc      | 120.0             | 2025-01-15  | 2024-12-01
```

---

### ProjectAllocations
Daily hour allocations linking projects to squads.

**Columns:**
- `AllocationId` (int, PK): Unique identifier for the allocation
- `ProjectId` (int, FK): References Projects.ProjectId
- `SquadId` (int, FK): References Squads.SquadId
- `AllocationDate` (date): The specific date for this allocation
- `AllocatedHours` (decimal): Number of hours allocated on this date
- `CreatedDate` (datetime): When allocation was created
- `AllocationType` (string): Either "Development" or "Onsite"

**Relationships:**
- Belongs to one `Project` (via ProjectId)
- Belongs to one `Squad` (via SquadId)

**Important Notes:**
- This is the LARGEST table (thousands of rows for daily allocations)
- Always filter by date range to avoid slow queries
- Use DISTINCT when joining to avoid duplicate results

**Example Data:**
```sql
AllocationId | ProjectId | SquadId | AllocationDate | AllocatedHours | AllocationType
-------------|-----------|---------|----------------|----------------|---------------
1            | 1         | 1       | 2024-11-01     | 8.0            | Development
2            | 1         | 1       | 2024-11-02     | 8.0            | Development
3            | 1         | 1       | 2024-11-25     | 4.0            | Onsite
```

---

### OnsiteSchedules
Onsite/UAT scheduling for projects.

**Columns:**
- `OnsiteScheduleId` (int, PK): Unique identifier for the schedule
- `ProjectId` (int, FK): References Projects.ProjectId
- `WeekStartDate` (date): Start date of the onsite week
- `EngineerCount` (int): Number of engineers needed onsite
- `TotalHours` (int): Total hours allocated for the onsite week (e.g., 40 hours)
- `OnsiteType` (string): Type of onsite work (e.g., "UAT", "GoLive")
- `CreatedDate` (datetime): When schedule was created
- `UpdatedDate` (datetime, nullable): When schedule was last updated

**Relationships:**
- Belongs to one `Project` (via ProjectId)

**Important Notes:**
- OnsiteSchedules does NOT link directly to Squads
- To find which squads are involved in onsite work, JOIN: OnsiteSchedules → Projects → ProjectAllocations → Squads

---

## Business Rules

### Capacity Calculation
```
Daily Squad Capacity = SUM(TeamMembers.DailyCapacityHours WHERE IsActive = 1 AND SquadId = X)
Daily Allocated Hours = SUM(ProjectAllocations.AllocatedHours WHERE SquadId = X AND AllocationDate = Y)
Remaining Capacity = Daily Squad Capacity - Daily Allocated Hours
Utilization % = (Daily Allocated Hours / Daily Squad Capacity) × 100
```

### Date Calculations
- **CRP Date** = 3 working days before UAT Date
- **UAT Date** = When development work completes
- **Go-Live Date** = UAT Date + 10 working days (2 weeks)
- **Working Days** = Monday-Friday (weekends excluded)

### Allocation Types
- **Development**: Work from StartDate to CRPDate (blue bars in UI)
- **Onsite**: Work from CRPDate+1 to GoLiveDate (orange bars in UI)

### Capacity Status Colors
- **Green**: <80% utilization (healthy)
- **Orange**: 80-100% utilization (high)
- **Red**: >100% utilization (overallocated)

---

## Common Query Patterns

### Get all active squads
```sql
SELECT SquadId, SquadName, SquadLeadName
FROM Squads
WHERE IsActive = 1
```

### Get squad capacity for a specific squad
```sql
SELECT s.SquadName, SUM(tm.DailyCapacityHours) as TotalCapacity
FROM Squads s
LEFT JOIN TeamMembers tm ON s.SquadId = tm.SquadId AND tm.IsActive = 1
WHERE s.SquadId = 1
GROUP BY s.SquadName
```

### Get projects for a specific squad
```sql
SELECT DISTINCT p.ProjectNumber, p.CustomerName, p.StartDate, p.GoLiveDate
FROM Projects p
JOIN ProjectAllocations pa ON p.ProjectId = pa.ProjectId
WHERE pa.SquadId = 1
ORDER BY p.StartDate
```

### Get squad utilization for a date range
```sql
SELECT
    s.SquadName,
    SUM(tm.DailyCapacityHours) as TotalCapacity,
    SUM(pa.AllocatedHours) as TotalAllocated,
    (SUM(pa.AllocatedHours) / NULLIF(SUM(tm.DailyCapacityHours), 0)) * 100 as UtilizationPercent
FROM Squads s
LEFT JOIN TeamMembers tm ON s.SquadId = tm.SquadId AND tm.IsActive = 1
LEFT JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId
    AND pa.AllocationDate BETWEEN '2024-11-01' AND '2024-11-30'
WHERE s.IsActive = 1
GROUP BY s.SquadName
```

### Get team members for a squad
```sql
SELECT TeamMemberId, MemberName, Role, DailyCapacityHours
FROM TeamMembers
WHERE SquadId = 1 AND IsActive = 1
ORDER BY
    CASE Role
        WHEN 'Squad Lead' THEN 1
        WHEN 'Project Lead' THEN 2
        WHEN 'Developer' THEN 3
        ELSE 4
    END
```

---

## Important Notes for Query Generation

1. **Always filter by IsActive = 1** when querying Squads or TeamMembers unless specifically asked for inactive ones
2. **Use date ranges** when querying ProjectAllocations to avoid scanning entire table
3. **Use DISTINCT** when joining ProjectAllocations to Projects to avoid duplicates
4. **NULLIF** is important in division to avoid divide-by-zero errors
5. **Date format**: Use 'YYYY-MM-DD' format for dates
6. **Weekends**: Saturday and Sunday are not working days
7. **Time periods**:
   - "This week" = Current Monday through Friday
   - "Next week" = Next Monday through Friday
   - "This month" = Current calendar month
   - "Q1" = January-March, "Q2" = April-June, "Q3" = July-September, "Q4" = October-December

## SQL Server Syntax (CRITICAL)

**This database uses Microsoft SQL Server. You MUST use SQL Server syntax:**

1. **Use TOP instead of LIMIT**:
   - CORRECT: `SELECT TOP 10 * FROM Squads`
   - WRONG: `SELECT * FROM Squads LIMIT 10`

2. **Use GETDATE() for current date**:
   - CORRECT: `SELECT GETDATE()`
   - WRONG: `SELECT NOW()` or `SELECT CURRENT_DATE`

3. **Use DATEADD() for date arithmetic**:
   - CORRECT: `SELECT DATEADD(WEEK, 2, GETDATE())`
   - WRONG: `SELECT DATE_ADD(NOW(), INTERVAL 2 WEEK)`

4. **Use DATEDIFF() for date differences**:
   - CORRECT: `SELECT DATEDIFF(DAY, StartDate, EndDate)`
   - WRONG: `SELECT TIMESTAMPDIFF(DAY, StartDate, EndDate)`

5. **String concatenation**:
   - CORRECT: `SELECT SquadName + ' - ' + SquadLeadName`
   - ALSO CORRECT: `SELECT CONCAT(SquadName, ' - ', SquadLeadName)`

6. **Common date operations**:
   - Today: `CAST(GETDATE() AS DATE)`
   - Two weeks from today: `DATEADD(WEEK, 2, CAST(GETDATE() AS DATE))`
   - Start of week: `DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0)`
   - End of month: `EOMONTH(GETDATE())`

7. **ALWAYS use table aliases in JOINs**:
   - CORRECT: `SELECT s.SquadId, s.SquadName, pa.AllocatedHours FROM Squads s JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId`
   - WRONG: `SELECT SquadId, SquadName FROM Squads JOIN ProjectAllocations ON SquadId = SquadId` (ambiguous columns!)
   - Always qualify column names with table aliases (e.g., `s.SquadId`, `pa.ProjectId`) to avoid ambiguity errors

---

## Safety Rules

**ONLY SELECT queries are allowed. Never generate:**
- INSERT, UPDATE, DELETE statements
- DROP, ALTER, TRUNCATE statements
- EXEC or stored procedure calls
- Multiple statements (no semicolons)
- System tables or views
- xp_ commands
- UNION queries (can be used for SQL injection)
