# What's New - Squad & Team Member Management

## New Features Added ✨

### 1. Squad Management (Full CRUD)
You can now:
- ✅ **Create** new squads
- ✅ **Edit** squad name and squad lead
- ✅ **Delete** squads (with safety check for allocations)
- ✅ View all squads with their capacity and team size

**How to Access:**
Click "**Manage Squads**" button in the header

### 2. Team Member Management
You can now:
- ✅ **Add** team members to any squad
- ✅ **Edit** team member details (name, role, capacity)
- ✅ **Remove** team members from squads
- ✅ Assign roles: Squad Lead, Project Lead, or Developer
- ✅ **Set individual daily capacity per person** (0-24 hours)

**How to Access:**
1. Click "**Manage Squads**"
2. Find your squad
3. Click "**Manage Team**" button

### 3. Individual Capacity Customization ⭐
**This was specifically requested!**

You can now set different daily capacity for each team member based on:
- Performance level (high performers: 7-8 hrs/day)
- Part-time status (4 hrs/day)
- Experience level (new team members: 5-6 hrs/day)
- Standard: 6.5 hrs/day

**Capacity ranges from 0 to 24 hours per day.**

### 4. Better Empty State Handling
- When no squads exist, you'll see a helpful message
- "Create First Squad" button appears to get you started
- No more confusion about missing data

## How It Works

### Creating Your First Squad

1. **Launch the app** and click "**Manage Squads**" (or "Create First Squad" if empty)
2. **Add a squad:**
   - Squad Name: "Squad Alpha"
   - Squad Lead Name: "John Doe"
   - Click **Create**

3. **Add team members:**
   - Click "**Manage Team**" on your squad card
   - Click "**+ Add Team Member**"
   - Fill in:
     - Name: "Jane Smith"
     - Role: Select from dropdown
     - Daily Capacity: Adjust based on person (default 6.5)
   - Click **Add**

4. **Repeat** for all team members

### Example Team Setup

**Squad Alpha** (71.5 hrs/day total)
- John Doe - Squad Lead - 6.5 hrs/day
- Sarah Johnson - Project Lead - 7.0 hrs/day (high performer)
- Mike Chen - Project Lead - 6.5 hrs/day
- Lisa Wang - Project Lead - 6.0 hrs/day
- 5 Developers @ 6.5 hrs/day each = 32.5 hrs
- 3 Developers @ 7.0 hrs/day each = 21.0 hrs (high performers)

### Editing Team Member Capacity

1. Go to Squad Management → Manage Team
2. Find the team member
3. Click ✏️ (edit icon)
4. Change the "Daily Capacity (hours)" field
5. Click **Update**
6. Capacity immediately reflects in Gantt charts!

## Technical Details

### New API Endpoints

**Squads:**
- `POST /api/squads` - Create squad
- `PUT /api/squads/{id}` - Update squad
- `DELETE /api/squads/{id}` - Delete squad (soft delete)

**Team Members:**
- All CRUD operations already existed, now fully integrated in UI

### New React Components

1. **SquadManagement.jsx** - Main squad management interface
2. **TeamMemberManagement.jsx** - Team member editing interface

### Database Changes

No schema changes needed! The database already supported these operations. We just added the UI to make it accessible.

## User Interface Updates

### Header
- Added "**Manage Squads**" button (gray button, left of "New Project")

### Squad Management Modal
- Large modal with squad cards
- Add/Edit/Delete operations
- "Manage Team" button per squad
- Shows capacity and team size

### Team Member Management
- Organized by role (Squad Leads, Project Leads, Developers)
- Cards showing name, role, and capacity
- Easy edit/delete per member
- Capacity badges with color coding
- Back button to return to squads

### Empty States
- Helpful messages when no squads exist
- Quick action buttons to get started

## Benefits

1. **No more manual database editing** - Everything is in the UI
2. **Individual capacity adjustment** - Account for different performance levels
3. **Real-time capacity updates** - Changes immediately reflect in Gantt charts
4. **Safe deletion** - Can't delete squads with active allocations
5. **Role organization** - Easy to see squad structure at a glance

## Next Steps

1. **Run the setup** if you haven't already: `.\setup.bat`
2. **Start the app**: `.\start.bat`
3. **Create your squads** using "Manage Squads"
4. **Add team members** with appropriate capacities
5. **Start allocating projects!**

## Migration from Old System

If you previously had squads in the database from seed data:
- They should still be there
- You can now edit them through the UI
- Add/remove team members as needed
- Adjust individual capacities

## Tips

💡 **Performance-based capacity:**
- Senior devs: 7-8 hrs/day
- Mid-level: 6.5 hrs/day
- Junior: 5-6 hrs/day
- Part-time: 4 hrs/day

💡 **Starting fresh?**
Delete the database and recreate:
```bash
dotnet ef database drop
dotnet ef database update
```
Then add squads through the UI!

💡 **Testing capacity changes:**
1. Create a project with 160 hours
2. Allocate to a squad
3. Note the daily allocation
4. Edit a team member's capacity
5. Reallocate the project
6. See the hours adjust!

---

**All your requirements are now implemented!** ✅

Enjoy your fully-featured project scheduling system! 🎉
