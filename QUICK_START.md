# Quick Start Guide

## First Time Setup

1. **Run the setup script:**
   ```bash
   setup.bat
   ```
   This will:
   - Install all .NET and npm dependencies
   - Create the database
   - Seed initial data (3 squads with team members)

2. **Start the application:**
   ```bash
   start.bat
   ```
   This will:
   - Launch the backend API (port 5000)
   - Launch the frontend (port 3000)
   - Open your browser automatically

## Manual Setup (Alternative)

### Backend
```bash
# Install dependencies
dotnet restore

# Setup database
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run API
dotnet run
```

### Frontend
```bash
cd frontend
npm install
npm run dev
```

## Quick Usage Tutorial

### 1. Create Your First Project (30 seconds)
1. Click "**+ New Project**"
2. Fill in:
   - Project Number: `PROJ-001`
   - Customer Name: `Acme Corp`
   - Estimated Dev Hours: `320`
   - Estimated Onsite Hours: `40`
   - CRP Date: (select a date 4 weeks from now)
3. Click "**Create**"

### 2. Allocate to a Squad (10 seconds)
1. In the "Unallocated Projects" section, click "**Allocate**" next to your project
2. Watch it appear in the Gantt chart with hours spread across working days!

### 3. View Capacity
- Look at the **Capacity** row to see daily utilization
- Green bars = good capacity
- Orange bars = getting full
- Red bars = over-allocated

### 4. Reschedule a Project (drag & drop)
1. Click and drag a project bar in the Gantt chart
2. Drop it on a new start date
3. Done!

## Default Setup

After setup, you'll have:

**3 Squads:**
- Squad Alpha
- Squad Beta
- Squad Gamma

**Per Squad:**
- 1 Squad Lead
- 3 Project Leads
- 8 Developers
- **Total: 11 members × 6.5 hrs/day = 71.5 hours/day capacity**

## Common Tasks

### Add Team Member
```http
POST /api/teammembers
{
  "squadId": 1,
  "memberName": "John Doe",
  "role": "Developer",
  "dailyCapacityHours": 6.5,
  "isActive": true
}
```

### Export All Projects
Click "**Export CSV**" or "**Export JSON**" in the header

### View API Documentation
Navigate to: `http://localhost:5000/swagger`

## Troubleshooting

**Problem:** Database errors
**Solution:** Run `dotnet ef database drop` then `dotnet ef database update`

**Problem:** Frontend can't connect
**Solution:** Make sure backend is running on port 5000

**Problem:** npm install fails
**Solution:** Delete `frontend/node_modules` and `frontend/package-lock.json`, then run `npm install` again

**Problem:** dotnet ef not found
**Solution:** Install it with `dotnet tool install --global dotnet-ef`

## Key Concepts

**CRP Date** = Code Review/Code Complete date. This is when development work ends, used to calculate daily hour allocation.

**Capacity** = Total available hours per day. Calculated as: (Active Team Members) × (Daily Capacity Hours)

**Allocation** = Assigning project hours to a squad. Hours are automatically spread evenly across working days from Start Date to CRP Date.

**Working Days** = Weekdays only (weekends are automatically excluded)

## Tips

1. **Set realistic CRP dates** - Make sure there are enough working days between start and CRP
2. **Monitor capacity bars** - Keep squads under 80% for flexibility
3. **Use drag & drop** - Fastest way to reschedule projects
4. **Link Jira tickets** - Keep everything connected
5. **Export regularly** - Backup your planning data

## Architecture Overview

```
┌─────────────────┐
│  React Frontend │  Port 3000
│   (Vite + React)│
└────────┬────────┘
         │ HTTP
         ↓
┌─────────────────┐
│  ASP.NET Core   │  Port 5000
│    Web API      │
└────────┬────────┘
         │ EF Core
         ↓
┌─────────────────┐
│   SQL Server    │
│     LocalDB     │
└─────────────────┘
```

## Next Steps

- Read the full [README.md](README.md) for detailed documentation
- Check [DatabaseSchema.md](DatabaseSchema.md) for database details
- Explore the API at `http://localhost:5000/swagger`
- Customize squad names and team members via the API

Happy planning! 🚀
