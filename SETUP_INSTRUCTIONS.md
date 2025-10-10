# Setup Instructions

## First Time Setup

### Option 1: Run Setup Script (Recommended)
```bash
.\setup.bat
```

### Option 2: Manual Setup

If the setup script fails, follow these steps:

#### 1. Install .NET EF Tools
```bash
dotnet tool install --global dotnet-ef
```

#### 2. Restore Backend Dependencies
```bash
cd c:\Learning\DEMO
dotnet restore
```

#### 3. Install Frontend Dependencies
```bash
cd frontend
npm install
cd ..
```

#### 4. Create Database
```bash
# Create migration
dotnet ef migrations add InitialCreate

# Apply migration and create database
dotnet ef database update
```

## Running the Application

### Option 1: Use Start Script
```bash
.\start.bat
```

### Option 2: Manual Start

**Terminal 1 - Backend:**
```bash
cd c:\Learning\DEMO
dotnet run
```

**Terminal 2 - Frontend:**
```bash
cd c:\Learning\DEMO\frontend
npm run dev
```

Then open your browser to `http://localhost:3000`

## Initial Setup in Application

Since you mentioned the database doesn't have squads, here's what to do after the app starts:

### 1. Create Your First Squad
1. Click "**Manage Squads**" button in the header
2. Click "**+ Add Squad**"
3. Enter:
   - Squad Name: e.g., "Squad Alpha"
   - Squad Lead Name: e.g., "John Doe"
4. Click "**Create**"

### 2. Add Team Members to Squad
1. In the squad card, click "**Manage Team**"
2. Click "**+ Add Team Member**"
3. Fill in:
   - Name: e.g., "Jane Smith"
   - Role: Select "Squad Lead", "Project Lead", or "Developer"
   - Daily Capacity: e.g., 6.5 (adjust based on individual productivity)
4. Click "**Add**"
5. Repeat for all team members

### 3. Recommended Team Structure
For each squad, create:
- **1 Squad Lead** (role: Squad Lead) - 6.5 hours/day
- **3 Project Leads** (role: Project Lead) - 6.5 hours/day each
- **8 Developers** (role: Developer) - 6.5 hours/day each (adjust per person)

**Total per squad:** 11 members = ~71.5 hours/day capacity

### 4. Individual Capacity Adjustments
You can customize daily capacity for each person:
- High performers: 7-8 hours/day
- Part-time: 4 hours/day
- New team members: 5-6 hours/day
- Standard: 6.5 hours/day

## Troubleshooting

### "No squads yet" message
This is normal on first run. Click "Create First Squad" or "Manage Squads" to add squads.

### npm install errors
Try:
```bash
cd frontend
rm -rf node_modules package-lock.json
npm install
```

### Database errors
Reset database:
```bash
dotnet ef database drop
dotnet ef database update
```

### Port conflicts
- Backend uses port 5000
- Frontend uses port 3000
Make sure these ports are available.

## Quick Test

After setup:
1. Create a squad with 11 members
2. Create a project with 160 dev hours, CRP date 4 weeks out
3. Allocate project to squad
4. View Gantt chart showing daily allocations (~8 hrs/day)
5. Edit a team member's capacity and see it update

## Key Features Added

✅ **Squad Management**
- Add/Edit/Delete squads
- View squad capacity and team size
- Soft delete (prevents deletion if allocations exist)

✅ **Team Member Management**
- Add/Edit/Remove team members
- Assign roles (Squad Lead, Project Lead, Developer)
- **Individual capacity settings per person**
- Grouped by role for easy viewing
- View total squad capacity

✅ **Capacity Customization**
- Set capacity from 0-24 hours per day
- Adjust per individual performance
- Real-time capacity calculation
- Capacity reflected in Gantt chart

## Next Steps

Once you have squads and team members:
1. Create projects with CRP dates
2. Allocate projects to squads
3. View capacity utilization in Gantt charts
4. Drag and drop to reschedule
5. Export data as needed

Enjoy your project scheduling system! 🚀
