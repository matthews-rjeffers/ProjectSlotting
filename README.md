# Project Scheduler

A squad capacity planning system for managing software development team capacity and project allocations.

## What Does This Project Do?

Project Scheduler helps software development managers:

1. **Manage Squad Capacity** - Track team members, their roles, and daily available hours
2. **Plan Project Timelines** - Schedule projects with automatic date calculations for development, UAT, and go-live milestones
3. **Visualize Allocations** - See team capacity utilization over time with color-coded status indicators
4. **Optimize Resource Allocation** - Use intelligent algorithms to find optimal project start dates based on available capacity
5. **Track Onsite Schedules** - Manage which team members are assigned to onsite/UAT work

## Key Features

### Squad Management
- Create and manage multiple development squads
- Track team members with roles (Squad Lead, Project Lead, Developer)
- Set daily capacity hours per team member
- Mark members as active/inactive

### Project Planning
- Create projects with customer info, required hours, and key dates
- Automatic date calculations:
  - **CRP Date**: 3 working days before UAT
  - **UAT Date**: When development work completes
  - **Go-Live Date**: 2 weeks (10 working days) after UAT
- Weekend exclusion in all date calculations

### Smart Allocation Algorithms

**Greedy Algorithm** (Default)
- Fills partial capacity each day
- Can start projects earlier by spreading hours across available capacity
- Minimum 2 hours per day allocation
- Maximum 180 working days duration

**Strict Algorithm**
- Requires full daily capacity to be available
- More predictable staffing patterns
- Won't allocate unless entire team capacity is free

### Capacity Visualization

**Weekly Aggregated View**
- Shows capacity utilization by week
- Color-coded status:
  - **Green**: <80% utilization (normal)
  - **Orange**: 80-100% utilization (high)
  - **Red**: >100% utilization (overloaded)
- Summary metrics: Team Size, Daily Capacity, Overall Utilization, Active Projects

**Squad Level Gantt Chart**
- Timeline view of all projects by squad
- Shows development phases (blue bars) and onsite phases (orange bars)
- Key milestones: Start Date, CRP, UAT, Go-Live
- Filterable by squad
- Zoomable timeline: Day, Week, or Month view
- Adjustable date range

### Onsite Schedule Management
- Assign team members to UAT and Go-Live onsite work
- Track total hours and engineer count per phase
- Edit assignments and hours as needed

### Natural Language Query (AI/RAG)
- Ask questions about capacity in plain English
- Query project allocations, squad availability, and timelines
- Powered by OpenAI GPT-4o-mini with RAG (Retrieval-Augmented Generation)

## How to Use

### Initial Setup

1. **Install Dependencies**
   ```bash
   # Install .NET 8.0 SDK (if not already installed)
   # Install Node.js 18+ (if not already installed)

   # Restore backend packages
   dotnet restore

   # Install frontend packages
   cd frontend
   npm install
   cd ..
   ```

2. **Setup Database**
   ```bash
   # Apply database migrations (creates SQL Server LocalDB database)
   dotnet ef database update
   ```

3. **Start the Application**
   ```bash
   # Option 1: Use the batch file (Windows)
   start.bat

   # Option 2: Start manually in separate terminals
   # Terminal 1 - Backend
   dotnet run

   # Terminal 2 - Frontend
   cd frontend
   npm run dev
   ```

4. **Access the Application**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5000

### Setting Up Your First Squad

1. Navigate to **Teams** page
2. Click **"+ Add New Squad"**
3. Enter squad name (e.g., "Alpha Squad")
4. Click **Save**

### Adding Team Members

1. On the **Teams** page, find your squad
2. Click **"Manage Team Members"**
3. Click **"+ Add Team Member"**
4. Enter details:
   - **Name**: Team member's name
   - **Role**: Squad Lead, Project Lead, or Developer
   - **Daily Capacity Hours**: Hours available per day (e.g., 6)
   - **Is Active**: Check to include in capacity calculations
5. Click **Save**
6. Repeat for all team members

**Example Team Setup:**
- John Smith - Squad Lead - 6 hours/day
- Jane Doe - Project Lead - 6 hours/day
- Bob Johnson - Developer - 7 hours/day
- Alice Williams - Developer - 7 hours/day

**Total Squad Capacity**: 26 hours/day

### Creating a Project

1. Navigate to **Projects** page
2. Click **"+ Add New Project"**
3. Enter project details:
   - **Project Number**: Internal reference (e.g., "PROJ-001")
   - **Customer Name**: Client name
   - **Total Required Hours**: Total development hours needed (e.g., 520)
   - **Go-Live Date**: Target production date
   - **Buffer Percentage**: Optional extra capacity buffer (default: 0%)
4. Click **Save**

The system will automatically calculate:
- **UAT Date**: Based on available capacity and required hours
- **CRP Date**: 3 working days before UAT

### Using Schedule Suggestion

For projects without a CRP date:

1. Click **"Suggest Schedule"** button on the project card
2. Choose allocation algorithm:
   - **Greedy**: Flexible, uses partial capacity
   - **Strict**: Requires full capacity availability
3. Review the suggested dates:
   - Start Date
   - CRP Date (3 days before UAT)
   - UAT Date (when dev completes)
   - Go-Live Date (2 weeks after UAT)
4. Click **"Preview Allocation"** to see day-by-day allocations
5. Click **"Apply Schedule"** to accept the suggestion

The system will automatically:
- Create development phase allocations
- Create UAT onsite schedule (1 engineer)
- Create Go-Live onsite schedule (1 engineer)

### Assigning Project to Squad

For projects with a CRP date already set:

1. Click the **"Assign to squad..."** dropdown
2. Select a squad from the list
3. The project will be allocated using the greedy algorithm

### Viewing Capacity Utilization

**Weekly View** (Capacity Page):
1. Navigate to **Capacity** page
2. Select a squad from the dropdown
3. View weekly capacity cards showing:
   - Week range
   - Daily capacity available
   - Hours allocated
   - Remaining capacity
   - Utilization percentage with color coding

**Gantt Chart View**:
1. Navigate to **Gantt** page
2. Optionally filter by squad
3. Adjust zoom level (Day/Week/Month)
4. Adjust date range if needed
5. View projects with:
   - Blue bars: Development phases
   - Orange bars: Onsite phases (UAT/Go-Live)
   - Milestone markers: Start, CRP, UAT, Go-Live dates

### Managing Onsite Schedules

1. On the **Projects** page, click **"Manage Onsite"** on a project
2. View existing UAT and Go-Live schedules
3. Click **Edit** on a schedule to modify:
   - Add/remove team members
   - Adjust hours per member
   - Change total hours or engineer count
4. Click **Save** to apply changes

### Using Natural Language Query

1. Navigate to **Capacity** page
2. Click on the chat/query interface
3. Ask questions in plain English:
   - "Which squads have capacity available next week?"
   - "When can Project ABC be scheduled?"
   - "Show me all projects for Customer XYZ"
   - "What is the utilization for Squad Alpha in December?"
4. The AI will query the database and provide natural language responses

### Understanding Capacity Colors

- **Green (<80%)**: Squad has healthy available capacity
- **Orange (80-100%)**: Squad is at high utilization, limited flexibility
- **Red (>100%)**: Squad is overallocated, needs attention

### Editing or Deleting Projects

**Edit Project**:
1. Click **Edit** button on project card
2. Modify details
3. Click **Save**
4. Note: Changing hours or dates may require re-allocation

**Delete Project**:
1. Click **Delete** button on project card
2. Confirm deletion
3. All associated allocations and onsite schedules will be removed

### Editing or Deleting Team Members

**Edit Team Member**:
1. Click **Edit** on team member row
2. Modify name, role, capacity, or active status
3. Click **Save**

**Delete Team Member**:
1. Click **Delete** on team member row
2. Confirm deletion
3. Member is permanently removed from the squad

### Deleting Squads

1. On the **Squads** page, click **Delete** on a squad card
2. **Note**: Cannot delete squads with active team members
3. Remove or deactivate all team members first, then delete the squad

## Common Workflows

### Workflow 1: Planning a New Project

1. Create the project with customer name, hours, and go-live date
2. Click "Suggest Schedule" to find optimal start date
3. Review the suggested timeline
4. Preview the allocation to see daily hour distribution
5. Apply the schedule to create allocations
6. View capacity page to confirm utilization is acceptable
7. Adjust onsite schedules if needed to assign specific team members

### Workflow 2: Adding Capacity to Overloaded Squad

1. Identify overloaded week (red capacity card)
2. Navigate to Teams page
3. Add new team member or increase existing member's daily hours
4. Return to Capacity page to verify utilization improved

### Workflow 3: Rescheduling a Project

1. Navigate to Projects page
2. Find the project that needs rescheduling
3. Click "Suggest Schedule" again
4. The system will find a new optimal start date
5. Apply the new schedule (replaces old allocations)

### Workflow 4: Viewing All Projects Timeline

1. Navigate to Gantt page
2. Select "All Squads" or specific squad
3. Choose appropriate zoom level:
   - Day view: Short-term detailed planning
   - Week view: Mid-term overview
   - Month view: Long-term strategic view
4. Scroll timeline to see all phases and milestones

### Workflow 5: Using AI to Answer Questions

1. Navigate to Capacity page
2. Open the natural language query interface
3. Ask specific questions about capacity, projects, or schedules
4. Review the AI's response and supporting data
5. Use insights to make informed planning decisions

## Technical Architecture

### Backend (ASP.NET Core)
- **Language**: C# with .NET 8.0
- **Database**: SQL Server LocalDB with Entity Framework Core
- **API**: RESTful Web API on http://localhost:5000

**Key Controllers**:
- `ProjectsController` - Project CRUD, allocation, schedule suggestion, gantt data
- `SquadsController` - Squad CRUD, capacity queries
- `TeamMembersController` - Team member CRUD
- `OnsiteSchedulesController` - Onsite schedule management
- `QueryController` - Natural language query processing (AI/RAG)

**Key Services**:
- `AllocationService` - Greedy and strict allocation algorithms
- `CapacityService` - Daily capacity calculations
- `ScheduleSuggestionService` - Schedule suggestion logic
- `RagService` - RAG query processing with OpenAI integration

### Frontend (React + Vite)
- **Framework**: React 18 with functional components and hooks
- **Build Tool**: Vite
- **Dev Server**: http://localhost:3000

**Main Pages**:
- `/capacity` - Weekly capacity view with AI query interface
- `/projects` - Project management
- `/squads` - Squad management
- `/teams` - Team member management
- `/gantt` - Gantt chart timeline view

## Database Schema

```
Squads (SquadId PK)
  ├── TeamMembers (TeamMemberId PK, FK: SquadId)
  │     ├── MemberName
  │     ├── Role (Squad Lead, Project Lead, Developer)
  │     ├── DailyCapacityHours
  │     └── IsActive
  │
  └── Projects (ProjectId PK, FK: SquadId)
        ├── ProjectNumber
        ├── CustomerName
        ├── TotalRequiredHours
        ├── BufferPercentage
        ├── StartDate, CRPDate, UATDate, GoLiveDate
        │
        ├── ProjectAllocations (AllocationId PK, FK: ProjectId)
        │     ├── AllocationDate
        │     ├── AllocatedHours
        │     └── AllocationType (Development, Onsite)
        │
        └── OnsiteSchedules (ScheduleId PK, FK: ProjectId)
              ├── Type (UAT, GoLive)
              ├── StartDate
              ├── TotalHours
              ├── NumberOfEngineers
              └── AssignedMembers (JSON array)
```

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- SQL Server LocalDB (included with Visual Studio or SQL Server Express)
- Visual Studio 2022 or VS Code (recommended)

## Installation & Setup

See the "How to Use" section above for detailed setup instructions.

## API Endpoints

### Projects
- `GET /api/projects` - Get all projects
- `GET /api/projects/{id}` - Get project by ID
- `POST /api/projects` - Create new project
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Delete project
- `POST /api/projects/{id}/allocate` - Allocate project to squad
- `POST /api/projects/{id}/suggest-schedule` - Get schedule suggestion
- `GET /api/projects/gantt-data` - Get gantt chart data

### Squads
- `GET /api/squads` - Get all squads
- `GET /api/squads/{id}` - Get squad by ID
- `POST /api/squads` - Create new squad
- `PUT /api/squads/{id}` - Update squad
- `DELETE /api/squads/{id}` - Delete squad
- `GET /api/squads/{id}/capacity` - Get squad capacity by date range

### Team Members
- `GET /api/teammembers` - Get all team members
- `GET /api/teammembers/{id}` - Get team member by ID
- `POST /api/teammembers` - Create new team member
- `PUT /api/teammembers/{id}` - Update team member
- `DELETE /api/teammembers/{id}` - Delete team member

### Onsite Schedules
- `GET /api/onsiteschedules` - Get all onsite schedules
- `GET /api/onsiteschedules/{id}` - Get onsite schedule by ID
- `PUT /api/onsiteschedules/{id}` - Update onsite schedule

### Query (AI/RAG)
- `POST /api/query` - Submit natural language query
- Request body: `{ "question": "your question here" }`
- Returns natural language response with supporting data

## Development Commands

### Backend
```bash
dotnet run                          # Start backend API
dotnet build                        # Build project
dotnet restore                      # Restore NuGet packages
dotnet ef migrations add <Name>     # Create new migration
dotnet ef database update           # Apply migrations
dotnet ef database drop             # Drop database (destructive)
```

### Frontend
```bash
cd frontend
npm run dev                         # Start dev server
npm run build                       # Build for production
npm run preview                     # Preview production build
npm install                         # Install dependencies
```

## Configuration

### Database Connection
Edit `appsettings.json` to change database connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectSchedulerDb;Trusted_Connection=True;"
  }
}
```

### API Base URL
Edit `frontend/src/api.js` to change backend URL:
```javascript
const api = axios.create({
  baseURL: 'http://localhost:5000'
});
```

### OpenAI API Key (for AI/RAG feature)
Add to `appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

## Troubleshooting

### Backend won't start
- Ensure .NET 8.0 SDK is installed: `dotnet --version`
- Check if port 5000 is available
- Run `dotnet restore` to restore packages
- Check database connection string in appsettings.json

### Frontend won't start
- Ensure Node.js 18+ is installed: `node --version`
- Check if port 3000 is available
- Run `npm install` to install dependencies
- Check that backend is running on http://localhost:5000

### Database errors
- Run `dotnet ef database update` to apply migrations
- If LocalDB not installed, install SQL Server Express with LocalDB feature
- For persistent issues, drop and recreate: `dotnet ef database drop` then `dotnet ef database update`

### Allocation not working
- Verify squad has active team members with capacity hours > 0
- Check that project has required hours and go-live date set
- Ensure there's enough capacity available in the date range
- Try the greedy algorithm instead of strict for more flexibility

### Capacity showing 0%
- Verify team members are marked as "Active"
- Check that daily capacity hours are set > 0
- Ensure squad has at least one active team member

### AI Query not working
- Verify OpenAI API key is configured in appsettings.json
- Check backend logs for API errors
- Ensure you have valid OpenAI API credits

## Known Limitations

- Weekends (Saturday/Sunday) are excluded from calculations, but no holiday calendar support
- Buffer percentage field exists in project model but not yet integrated into UI
- Onsite schedules are auto-created with 1 engineer - adjust manually if needed
- Existing projects may need re-allocation after system updates to ensure correct onsite allocations
- AI/RAG feature requires OpenAI API key and internet connection

## Future Enhancements

Potential features for future development:
- Holiday calendar support for different regions
- Resource leveling across squads
- Project dependencies and critical path
- Export capacity reports to Excel/PDF
- Email notifications for capacity alerts
- Multi-project allocation optimization
- Historical capacity utilization reports
- Team member skills matrix and matching
- Vacation/PTO tracking integration
- Multi-tenant support for different organizations

## Technology Stack

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SQL Server LocalDB
- OpenAI API (for AI/RAG)

**Frontend:**
- React 18
- Vite 5
- Axios
- CSS3

## Support and Feedback

For issues, feature requests, or questions:
- Check [CLAUDE.md](CLAUDE.md) for AI-assisted development guidelines
- Check [MCP_SERVER_SETUP.md](MCP_SERVER_SETUP.md) for MCP server configuration
- Review git commit history for recent changes

## License

Internal use only - proprietary software.
