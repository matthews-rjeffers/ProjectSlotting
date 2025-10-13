# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**Project Scheduler** is a squad capacity planning system for managing software development team capacity and project allocations. It consists of an ASP.NET Core Web API backend and a React + Vite frontend.

## Development Commands

### Backend (ASP.NET Core)
```bash
# Run the backend API (from project root)
dotnet run                          # Starts on http://localhost:5000

# Database migrations
dotnet ef migrations add <Name>     # Create new migration
dotnet ef database update           # Apply pending migrations
dotnet ef database drop             # Drop database (destructive)

# Build and test
dotnet build
dotnet restore
```

### Frontend (React + Vite)
```bash
# Run the frontend dev server (from frontend/ directory)
cd frontend
npm run dev                         # Starts on http://localhost:3000

# Build for production
npm run build
npm run preview
```

### Quick Start
```bash
# Start both backend and frontend together (Windows)
start.bat
```

## Architecture

### Backend Structure
```
Controllers/
  ├── ProjectsController.cs        # Project CRUD, allocation, schedule suggestion
  ├── SquadsController.cs          # Squad CRUD, capacity queries
  ├── TeamMembersController.cs     # Team member CRUD
  └── OnsiteSchedulesController.cs # Onsite schedule management

Services/
  ├── AllocationService.cs         # Core allocation logic (greedy/strict algorithms)
  ├── CapacityService.cs           # Daily capacity calculations
  └── ScheduleSuggestionService.cs # Schedule suggestion algorithms

Models/
  ├── Squad.cs
  ├── TeamMember.cs
  ├── Project.cs
  ├── ProjectAllocation.cs
  └── OnsiteSchedule.cs

Data/
  └── ProjectSchedulerDbContext.cs # EF Core DbContext
```

### Frontend Structure
```
frontend/src/
  ├── App.jsx                      # Main app with React Router
  ├── api.js                       # Axios API client (base URL: http://localhost:5000)
  ├── pages/
  │   ├── CapacityPage.jsx         # /capacity - Main capacity planning view
  │   ├── ProjectsPage.jsx         # /projects - Project management
  │   ├── SquadsPage.jsx           # /squads - Squad management
  │   └── TeamsPage.jsx            # /teams - Team member management
  └── components/
      ├── ImprovedCapacityView.jsx # Weekly aggregated capacity view
      ├── ScheduleSuggestionModal.jsx # Schedule suggestion UI
      ├── AllocationPreviewModal.jsx  # Preview allocations before applying
      ├── ProjectForm.jsx          # Create/edit projects
      ├── SquadManagement.jsx      # Squad CRUD modal
      ├── TeamMemberManagement.jsx # Team member CRUD
      └── OnsiteScheduleManager.jsx # Onsite schedule management
```

### Database Schema
- **Squads** (SquadId PK) → has many TeamMembers, Projects
- **TeamMembers** (TeamMemberId PK) → belongs to Squad, has DailyCapacityHours
- **Projects** (ProjectId PK) → has many ProjectAllocations, OnsiteSchedules
- **ProjectAllocations** (AllocationId PK) → daily hour allocations per squad per project
- **OnsiteSchedules** (ScheduleId PK) → onsite/UAT scheduling

**Connection String**: SQL Server LocalDB (see appsettings.json)

## Key Concepts

### Allocation Algorithms
Two algorithms are available for project scheduling:

1. **Greedy Algorithm** (default): Fills partial capacity each day, can start earlier
   - Implementation: `TryFindFlexibleAllocationWindow()` in AllocationService
   - Uses `AllocationSchedule` helper class for variable daily allocations
   - Parameters: `minHoursPerDay = 2h`, `maxDurationDays = 180 working days`

2. **Strict Algorithm**: Requires full daily capacity, more predictable staffing
   - Implementation: `TryFindAllocationWindow()` in AllocationService
   - Won't allocate unless full team capacity is available

### Allocation Types
- **Development**: Start Date → CRP Date (blue bars in UI)
- **Onsite**: CRP Date + 1 → Go Live Date (orange bars in UI)
- Field: `AllocationType` enum in ProjectAllocation model

### Date Calculations
- **CRP Date**: 3 working days before UAT
- **UAT Date**: When dev work completes
- **Go-Live Date**: 2 weeks (10 working days) after UAT
- Weekends are automatically excluded from all calculations

### Capacity Calculation
```
Daily Squad Capacity = SUM(TeamMember.DailyCapacityHours WHERE IsActive = true)
Daily Allocated Hours = SUM(ProjectAllocation.AllocatedHours WHERE AllocationDate = X)
Remaining Capacity = Daily Capacity - Daily Allocated Hours
```

### Color-Coded Capacity Status
- **Green**: <80% utilization (normal)
- **Orange**: 80-100% utilization (high)
- **Red**: >100% utilization (overloaded)

## Important API Property Names

**CRITICAL**: The backend returns camelCase JSON. Always use:
- `squadId` (NOT SquadId)
- `projectId` (NOT ProjectId)
- `teamMemberId` (NOT memberId or TeamMemberId)
- `memberName`, `role`, `dailyCapacityHours`, `isActive`

## Recent Changes

### 2025-10-10: Flexible Allocation Algorithm
- Added greedy algorithm option that spreads hours across partial capacity
- Schedule Suggestion modal now has algorithm type dropdown
- Auto-creates UAT and Go-Live onsite schedules (1 engineer each)
- Projects without CRP date show "Suggest Schedule" button
- Projects with CRP date show "Assign to squad..." dropdown
- Fixed `getNextMonday()` calculation

### 2025-10-10: Squad Management Redesign
- Simplified SquadManagement modal (shows only add/edit form)
- Delete squad moved to main page with validation (checks for active team members)
- Modal auto-closes after create/update

### 2025-10-10: Bug Fixes
- Fixed allocation preview modal table alignment
- Fixed onsite schedule edit validation using DTO pattern (UpdateOnsiteScheduleDto)
- Fixed property serialization (removed navigation properties before API calls)
- Fixed ProjectForm auto-refresh by calling `onSave()` instead of `onCancel()`

### 2025-10-08: Navigation & UI Improvements
- Added React Router with 3 main pages (/capacity, /squads, /teams)
- Replaced day-by-day Gantt with weekly aggregated view
- Added summary metrics cards (Team Size, Daily Capacity, Utilization, Active Projects)
- Team members sorted by role (Squad Lead → Project Lead → Developer)
- Hard delete for team members (permanent removal)

## Common Development Patterns

### Adding a New Entity
1. Create model in `Models/`
2. Add DbSet to `ProjectSchedulerDbContext.cs`
3. Create migration: `dotnet ef migrations add Add<EntityName>`
4. Update database: `dotnet ef database update`
5. Create controller in `Controllers/`
6. Add API methods to `frontend/src/api.js`
7. Create React component/page

### Making Database Changes
1. Modify model class in `Models/`
2. Update `ProjectSchedulerDbContext.cs` if relationships changed
3. Create migration: `dotnet ef migrations add <DescriptiveName>`
4. Review generated migration in `Migrations/`
5. Apply: `dotnet ef database update`
6. For LocalDB issues, use sqlcmd for manual schema changes

### API Request Pattern
```javascript
// frontend/src/api.js uses axios with base URL
import api from './api';

// GET request
const response = await api.get('/api/squads');

// POST request
const response = await api.post('/api/projects', projectData);

// Always use camelCase for properties in request/response
```

### Callback Naming Conventions
- Use `onSuccess` for successful operations
- Use `onSave` for form save callbacks
- Use `onCancel` for cancel operations
- Component auto-refreshes parent data after mutations

## Known Issues

- Existing projects may need re-allocation to have correct onsite allocations (if they have Go-Live dates)
- Buffer percentage field exists but may need UI integration
- Weekend handling assumes Sat/Sun - no holiday calendar support yet

## MCP Servers Configured

The following MCP servers are available (see MCP_SERVER_SETUP.md):
1. sequential-thinking - Complex problem solving
2. filesystem - Enhanced file system access
3. puppeteer - Browser automation
4. memory - Persistent knowledge across sessions
5. fetch - HTTP requests and API testing
6. github - GitHub integration (requires token)
7. git - Version control operations
