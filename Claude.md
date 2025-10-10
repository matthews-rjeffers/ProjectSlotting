# Claude Memory

This file contains information for Claude to remember across conversations.

---

## Notes

### Project Structure
- **Backend**: ASP.NET Core Web API (C#) on port 5000
- **Frontend**: React + Vite on port 3000
- **Database**: SQL Server LocalDB

### Recent Changes (2025-10-08)

#### Navigation & Pages
- Added React Router for multi-page navigation
- Created 3 main pages:
  - **Capacity Planning** (`/capacity`) - Main scheduling and capacity view
  - **Manage Squads** (`/squads`) - Squad management with cards view
  - **Manage Teams** (`/teams`) - Team member CRUD operations

#### Capacity Planning Improvements
- Replaced day-by-day Gantt chart with **weekly aggregated view**
- Added **summary metrics cards**: Team Size, Daily Capacity, Overall Utilization, Active Projects
- Created **collapsible unallocated projects** section with card layout
- Implemented **color-coded capacity status**:
  - Green (normal): <80% utilization
  - Orange (high): 80-100% utilization
  - Red (overloaded): >100% utilization
- Weekly cards show:
  - Week date range
  - Utilization percentage with color badge
  - Progress bar with allocated/total hours
  - Available hours and project count
  - Expandable project list on click

#### Team Management
- Team members sorted by role (Squad Lead → Project Lead → Developer), then alphabetically by name
- Can change squad assignment when editing a team member
- Hard delete (permanent removal) instead of soft delete
- Fixed property name issue: `teamMemberId` (not `memberId`)

#### MCP Servers Configured
1. sequential-thinking - Complex problem solving
2. filesystem - Enhanced file system access
3. puppeteer - Browser automation
4. memory - Persistent knowledge across sessions
5. fetch - HTTP requests and API testing
6. github - GitHub integration (requires token)
7. git - Version control operations

### API Property Names (camelCase from backend)
- `squadId` ✓
- `projectId` ✓
- `teamMemberId` ✓ (NOT memberId)
- `memberName`, `role`, `dailyCapacityHours`, `isActive`

### Allocation Logic (2025-10-08 Update)
- **Dev Hours**: Allocated from Start Date to CRP Date
- **Onsite Hours**: Allocated from CRP Date + 1 to Go Live Date
- Each allocation has an `AllocationType` field: "Development" or "Onsite"
- Color coding in UI:
  - 🔵 Blue bars = Development hours
  - 🟠 Orange bars = Onsite hours
- Projects can be unassigned from the capacity view (removes all allocations)

### Known Issues / Future Improvements
- Existing projects need to be re-allocated to have correct onsite allocations (if they have Go Live dates)

