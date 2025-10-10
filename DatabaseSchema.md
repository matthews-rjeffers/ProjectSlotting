# Project Scheduling System - Database Schema

## Entity Relationship Overview

```
Squads (1) ---> (Many) TeamMembers
Squads (1) ---> (Many) Projects
Projects (1) ---> (Many) ProjectAllocations
```

## Tables

### 1. Squads
Represents each software development squad.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| SquadId | int | PK, Identity | Unique identifier |
| SquadName | nvarchar(100) | NOT NULL | Squad name (e.g., "Squad Alpha") |
| SquadLeadName | nvarchar(200) | NOT NULL | Name of squad lead |
| IsActive | bit | NOT NULL, Default=1 | Active status |

### 2. TeamMembers
Represents developers and project leads in each squad.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| TeamMemberId | int | PK, Identity | Unique identifier |
| SquadId | int | FK -> Squads.SquadId | Squad assignment |
| MemberName | nvarchar(200) | NOT NULL | Team member name |
| Role | nvarchar(50) | NOT NULL | "ProjectLead" or "Developer" |
| DailyCapacityHours | decimal(4,2) | NOT NULL, Default=6.5 | Available hours per day |
| IsActive | bit | NOT NULL, Default=1 | Active status |

### 3. Projects
Represents software development projects.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ProjectId | int | PK, Identity | Unique identifier |
| ProjectNumber | nvarchar(50) | NOT NULL, Unique | Project identifier |
| CustomerName | nvarchar(200) | NOT NULL | Customer name |
| CustomerCity | nvarchar(100) | NULL | Customer city |
| CustomerState | nvarchar(2) | NULL | Customer state (2-letter code) |
| EstimatedDevHours | decimal(10,2) | NOT NULL | Estimated software dev hours |
| EstimatedOnsiteHours | decimal(10,2) | NOT NULL | Estimated onsite hours |
| GoLiveDate | datetime2 | NULL | Go-live date |
| CRPDate | datetime2 | NOT NULL | Code Review/CRP date (code complete) |
| UATDate | datetime2 | NULL | User Acceptance Testing date |
| JiraLink | nvarchar(500) | NULL | Link to Jira ticket |
| StartDate | datetime2 | NULL | Project start date |
| CreatedDate | datetime2 | NOT NULL, Default=GETDATE() | Record creation date |
| UpdatedDate | datetime2 | NULL | Record last update date |

### 4. ProjectAllocations
Represents assignment of projects to squads with daily hour allocations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AllocationId | int | PK, Identity | Unique identifier |
| ProjectId | int | FK -> Projects.ProjectId | Project reference |
| SquadId | int | FK -> Squads.SquadId | Squad assignment |
| AllocationDate | date | NOT NULL | Specific date for allocation |
| AllocatedHours | decimal(6,2) | NOT NULL | Hours allocated for this date |
| CreatedDate | datetime2 | NOT NULL, Default=GETDATE() | Record creation date |

**Unique Constraint:** (ProjectId, SquadId, AllocationDate)

## Key Design Decisions

1. **ProjectAllocations Table**: Stores daily hour allocations per project per squad. This allows:
   - Fine-grained capacity tracking per day
   - Easy querying for Gantt chart visualization
   - Flexible spreading of hours across project timeline

2. **Capacity Calculation**:
   - Daily squad capacity = SUM(TeamMembers.DailyCapacityHours) where SquadId = X and IsActive = 1
   - Daily allocated hours = SUM(ProjectAllocations.AllocatedHours) where SquadId = X and AllocationDate = Y
   - Remaining capacity = Daily capacity - Daily allocated hours

3. **Date Fields**: Using datetime2 for better precision and range (0001-01-01 to 9999-12-31)

4. **Soft Deletes**: IsActive flags allow historical data retention

## Indexes (Recommended)

```sql
-- For fast squad capacity queries
CREATE INDEX IX_TeamMembers_SquadId ON TeamMembers(SquadId) WHERE IsActive = 1;

-- For fast allocation lookups
CREATE INDEX IX_ProjectAllocations_SquadId_Date ON ProjectAllocations(SquadId, AllocationDate);
CREATE INDEX IX_ProjectAllocations_ProjectId ON ProjectAllocations(ProjectId);

-- For project lookups
CREATE INDEX IX_Projects_ProjectNumber ON Projects(ProjectNumber);
CREATE INDEX IX_Projects_CRPDate ON Projects(CRPDate);
```
