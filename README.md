# Project Scheduler - Squad Capacity Planning System

A comprehensive web application for managing and forecasting software development squad capacity and project allocations.

## Features

- **Squad Management**: Add/edit/delete squads with full CRUD operations
- **Team Member Management**: Manage squad leads, project leads, and developers with individual roles
- **Individual Capacity Settings**: Set custom daily capacity per team member (adjust for performance/part-time)
- **Project Tracking**: Track projects with customer info, dev hours, onsite hours, and key dates (CRP, UAT, Go-Live)
- **Capacity Planning**: Visual Gantt charts showing daily capacity allocation for each squad
- **Smart Allocation**: Automatically spreads project hours evenly from start date to CRP date
- **Drag & Drop Scheduling**: Easily reschedule projects by dragging them to new dates
- **2-Year Forecasting**: Plan capacity up to 2 years ahead
- **Export Functionality**: Export project data to CSV or JSON
- **Real-time Capacity Tracking**: See remaining unallocated hours per squad per day
- **Jira Integration**: Link projects to Jira tickets

## System Architecture

### Backend (ASP.NET Core 8 + EF Core)
- RESTful Web API
- SQL Server database with EF Core Model-First design
- Service layer for capacity calculations and allocation logic
- Automatic weekend skipping (only counts weekdays)
- 6.5 hours/day capacity per developer

### Frontend (React + Vite)
- Modern React application with Vite
- Interactive Gantt chart visualization
- Responsive design
- Real-time capacity monitoring
- Drag-and-drop project scheduling

## Database Schema

### Tables
1. **Squads** - Software development squads
2. **TeamMembers** - Developers and project leads in each squad
3. **Projects** - Project information and dates
4. **ProjectAllocations** - Daily hour allocations per project per squad

See [DatabaseSchema.md](DatabaseSchema.md) for detailed schema documentation.

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code (recommended)

## Installation & Setup

### 1. Backend Setup

```bash
# Navigate to the project directory
cd c:\Learning\DEMO

# Restore NuGet packages
dotnet restore

# Update the connection string in appsettings.json if needed
# Default: Server=(localdb)\\mssqllocaldb;Database=ProjectSchedulerDb;...

# Create and apply database migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the API (will start on https://localhost:5001 or http://localhost:5000)
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

### 2. Frontend Setup

```bash
# Navigate to the frontend directory
cd frontend

# Install dependencies
npm install

# Start the development server (will start on http://localhost:3000)
npm run dev
```

The application will open at `http://localhost:3000`

## Usage Guide

### 1. Creating Projects

1. Click "**+ New Project**" button in the header
2. Fill in project details:
   - **Project Number** (required, unique)
   - **Customer Name** (required)
   - **Customer City & State** (optional)
   - **Estimated Dev Hours** (required) - Total development hours needed
   - **Estimated Onsite Hours** (required) - Onsite/travel hours
   - **Start Date** - When work begins (defaults to today if not specified)
   - **CRP Date** (required) - Code Review/Code Complete date
   - **UAT Date** - User Acceptance Testing date
   - **Go-Live Date** - Production deployment date
   - **Jira Link** - Link to Jira ticket
3. Click "**Create**"

### 2. Allocating Projects to Squads

**Option A: Manual Allocation**
1. Find the project in the "Unallocated Projects" section
2. Click the "**Allocate**" button
3. System automatically spreads hours from Start Date to CRP Date

**Option B: Drag & Drop**
1. Drag a project from the "Unallocated Projects" section
2. Drop it onto any day in the Gantt chart
3. System recalculates allocation starting from that day

### 3. Rescheduling Projects

1. Drag an allocated project bar in the Gantt chart
2. Drop it on a new start date
3. System automatically reallocates hours from new start date to CRP date

### 4. Viewing Capacity

- **Capacity Row**: Shows daily capacity utilization
  - Green: < 80% utilized
  - Orange: 80-100% utilized
  - Red: > 100% over-allocated (not allowed during allocation)
- **Numbers**: Show "allocated hours / total capacity"
- **Projects Row**: Shows which projects are assigned each day

### 5. Managing Squads

- Click on different squads in the sidebar to view their capacity
- Each squad shows:
  - Squad name
  - Squad lead name
  - Number of active team members
  - Daily capacity (calculated automatically)

### 6. Adjusting Date Range

1. Use the "Date Range" controls in the sidebar
2. Set Start and End dates (up to 2 years)
3. Gantt chart updates automatically

### 7. Exporting Data

Click "**Export CSV**" or "**Export JSON**" in the header to download:
- All project information
- Customer details
- Hour estimates
- Key dates
- Squad allocations

### 8. Editing/Deleting Projects

- **Edit**: Click the ✏️ icon next to a project in the project list
- **Delete**: Click the 🗑️ icon (removes project and all allocations)

## Capacity Calculation Logic

### Daily Squad Capacity
```
Total Capacity = Sum of all active team members' daily capacity hours
Default: 11 members × 6.5 hours = 71.5 hours/day per squad
```

### Project Allocation
```
Working Days = Business days between Start Date and CRP Date (excludes weekends)
Hours Per Day = Estimated Dev Hours ÷ Working Days
```

The system ensures:
- Projects cannot be allocated if capacity is insufficient
- Hours are spread evenly across all working days
- Weekends are automatically excluded
- Over-allocation is prevented

## API Endpoints

### Squads
- `GET /api/squads` - Get all squads
- `GET /api/squads/{id}` - Get squad by ID
- `GET /api/squads/{id}/capacity?startDate={date}&endDate={date}` - Get capacity range
- `PUT /api/squads/{id}` - Update squad

### Projects
- `GET /api/projects` - Get all projects
- `GET /api/projects/{id}` - Get project by ID
- `POST /api/projects` - Create project
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Delete project
- `POST /api/projects/{id}/allocate` - Allocate project to squad
- `POST /api/projects/{id}/reallocate` - Reallocate project
- `POST /api/projects/can-allocate` - Check if allocation is possible

### Team Members
- `GET /api/teammembers?squadId={id}` - Get team members (optionally filtered by squad)
- `POST /api/teammembers` - Create team member
- `PUT /api/teammembers/{id}` - Update team member
- `DELETE /api/teammembers/{id}` - Soft delete team member

## Customization

### Adjusting Daily Capacity
Update team members' `DailyCapacityHours` in the database or via API:
```csharp
// Default is 6.5 hours per developer
PUT /api/teammembers/{id}
{
  "dailyCapacityHours": 7.0  // Adjust as needed
}
```

### Adding More Squads
```csharp
POST /api/squads
{
  "squadName": "Squad Delta",
  "squadLeadName": "John Doe",
  "isActive": true
}
```

### Modifying Team Structure
- Add/remove team members via the API
- Assign roles: "ProjectLead" or "Developer"
- Set custom capacity hours per individual

## Troubleshooting

### Database Issues
```bash
# Reset database
dotnet ef database drop
dotnet ef database update

# Create new migration after model changes
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Frontend Not Connecting to API
- Verify API is running on `http://localhost:5000`
- Check CORS settings in `Program.cs`
- Check proxy settings in `vite.config.js`

### Projects Won't Allocate
- Verify squad has sufficient capacity
- Check that CRP date is after start date
- Ensure working days exist between start and CRP dates
- Check browser console for errors

## Future Enhancements

Potential improvements:
- [ ] Holiday calendar support
- [ ] Multiple squad allocation per project
- [ ] Resource-level allocation (assign specific developers)
- [ ] Vacation/PTO tracking
- [ ] Historical capacity reporting
- [ ] Email notifications for over-allocation
- [ ] Integration with Azure DevOps or Jira API
- [ ] Mobile responsive optimization
- [ ] Dark mode theme

## Technology Stack

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SQL Server
- Swagger/OpenAPI

**Frontend:**
- React 18
- Vite 5
- Axios
- CSS3

## License

This project is for internal use. Customize as needed for your organization.

## Support

For questions or issues:
1. Check the database schema documentation
2. Review API endpoints in Swagger UI
3. Check browser console for frontend errors
4. Review backend logs for API errors
