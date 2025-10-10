import React, { useState, useEffect } from 'react';
import { getSquadCapacity, allocateProject, reallocateProject, getProjectAllocations } from '../api';
import './SquadGantt.css';

function SquadGantt({ squadId, squad, projects, dateRange, onProjectUpdate }) {
  const [capacityData, setCapacityData] = useState({});
  const [projectAllocations, setProjectAllocations] = useState({});
  const [selectedProject, setSelectedProject] = useState(null);
  const [draggedProject, setDraggedProject] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
  }, [squadId, dateRange, projects]);

  const loadData = async () => {
    setLoading(true);
    try {
      // Load capacity data
      const capacityResponse = await getSquadCapacity(
        squadId,
        dateRange.startDate,
        dateRange.endDate
      );
      setCapacityData(capacityResponse.data);

      // Load allocations for all projects
      const allocationsMap = {};
      for (const project of projects) {
        try {
          const allocResponse = await getProjectAllocations(project.projectId);
          const squadAllocations = allocResponse.data.filter(a => a.squadId === squadId);
          if (squadAllocations.length > 0) {
            allocationsMap[project.projectId] = squadAllocations;
          }
        } catch (error) {
          console.error(`Error loading allocations for project ${project.projectId}:`, error);
        }
      }
      setProjectAllocations(allocationsMap);
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const getDatesInRange = () => {
    const dates = [];
    const start = new Date(dateRange.startDate);
    const end = new Date(dateRange.endDate);

    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      // Skip weekends
      if (d.getDay() !== 0 && d.getDay() !== 6) {
        dates.push(new Date(d));
      }
    }

    return dates;
  };

  const getWeeksInRange = () => {
    const dates = getDatesInRange();
    const weeks = [];
    let currentWeek = null;

    dates.forEach(date => {
      const weekStart = getWeekStart(date);
      const weekKey = weekStart.toISOString().split('T')[0];

      if (!currentWeek || currentWeek.key !== weekKey) {
        currentWeek = {
          key: weekKey,
          start: weekStart,
          dates: []
        };
        weeks.push(currentWeek);
      }

      currentWeek.dates.push(date);
    });

    return weeks;
  };

  const getWeekStart = (date) => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Adjust when day is Sunday
    return new Date(d.setDate(diff));
  };

  const formatDate = (date) => {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  const getCapacityInfo = (date) => {
    const dateKey = date.toISOString().split('T')[0];
    return capacityData[dateKey] || { totalCapacity: 0, allocatedHours: 0, remainingCapacity: 0 };
  };

  const getProjectsForDate = (date) => {
    const dateKey = date.toISOString().split('T')[0];
    const projectsOnDate = [];

    Object.entries(projectAllocations).forEach(([projectId, allocations]) => {
      const allocation = allocations.find(a => a.allocationDate.split('T')[0] === dateKey);
      if (allocation) {
        const project = projects.find(p => p.projectId === parseInt(projectId));
        if (project) {
          projectsOnDate.push({
            project,
            allocation
          });
        }
      }
    });

    return projectsOnDate;
  };

  const handleAllocateProject = async (project) => {
    const startDate = project.startDate || new Date().toISOString().split('T')[0];

    try {
      await allocateProject(project.projectId, squadId, startDate);
      await loadData();
      onProjectUpdate();
      alert(`Project ${project.projectNumber} allocated successfully!`);
    } catch (error) {
      console.error('Error allocating project:', error);
      alert('Failed to allocate project. There may not be enough capacity.');
    }
  };

  const handleDragStart = (project, e) => {
    setDraggedProject(project);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDrop = async (date, e) => {
    e.preventDefault();

    if (!draggedProject) return;

    const newStartDate = date.toISOString().split('T')[0];

    try {
      await reallocateProject(draggedProject.projectId, squadId, newStartDate);
      await loadData();
      onProjectUpdate();
      setDraggedProject(null);
    } catch (error) {
      console.error('Error reallocating project:', error);
      alert('Failed to reallocate project. There may not be enough capacity.');
      setDraggedProject(null);
    }
  };

  const unallocatedProjects = projects.filter(p => !projectAllocations[p.projectId] || projectAllocations[p.projectId].length === 0);

  if (loading) {
    return <div className="gantt-loading">Loading...</div>;
  }

  const weeks = getWeeksInRange();
  const totalCapacity = squad?.teamMembers?.filter(tm => tm.isActive).reduce((sum, tm) => sum + tm.dailyCapacityHours, 0) || 0;

  return (
    <div className="squad-gantt">
      <div className="gantt-header">
        <h2>{squad?.squadName} - Capacity Planning</h2>
        <div className="gantt-info">
          <div className="info-item">
            <strong>Team Size:</strong> {squad?.teamMembers?.filter(tm => tm.isActive).length || 0} members
          </div>
          <div className="info-item">
            <strong>Daily Capacity:</strong> {totalCapacity.toFixed(1)} hours
          </div>
        </div>
      </div>

      {unallocatedProjects.length > 0 && (
        <div className="unallocated-projects">
          <h3>Unallocated Projects</h3>
          <div className="unallocated-list">
            {unallocatedProjects.map(project => (
              <div
                key={project.projectId}
                className="unallocated-project"
                draggable
                onDragStart={(e) => handleDragStart(project, e)}
              >
                <div className="project-info">
                  <strong>{project.projectNumber}</strong>
                  <span>{project.customerName}</span>
                  <span className="hours">{project.estimatedDevHours}h</span>
                </div>
                <button
                  onClick={() => handleAllocateProject(project)}
                  className="btn btn-small btn-primary"
                >
                  Allocate
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="gantt-chart">
        <div className="gantt-timeline">
          <div className="timeline-header">
            <div className="row-label">Week</div>
            {weeks.map(week => (
              <div key={week.key} className="week-header" style={{ gridColumn: `span ${week.dates.length}` }}>
                {formatDate(week.start)}
              </div>
            ))}
          </div>

          <div className="timeline-subheader">
            <div className="row-label">Day</div>
            {weeks.map(week =>
              week.dates.map(date => (
                <div key={date.toISOString()} className="day-header">
                  {date.toLocaleDateString('en-US', { weekday: 'short' })}
                  <br />
                  {date.getDate()}
                </div>
              ))
            )}
          </div>

          <div className="timeline-row capacity-row">
            <div className="row-label">Capacity</div>
            {weeks.map(week =>
              week.dates.map(date => {
                const capacity = getCapacityInfo(date);
                const utilizationPercent = capacity.totalCapacity > 0
                  ? (capacity.allocatedHours / capacity.totalCapacity) * 100
                  : 0;

                return (
                  <div
                    key={date.toISOString()}
                    className="capacity-cell"
                    onDragOver={handleDragOver}
                    onDrop={(e) => handleDrop(date, e)}
                  >
                    <div className="capacity-bar">
                      <div
                        className="capacity-fill"
                        style={{
                          width: `${Math.min(utilizationPercent, 100)}%`,
                          backgroundColor: utilizationPercent > 100 ? '#e74c3c' : utilizationPercent > 80 ? '#f39c12' : '#27ae60'
                        }}
                      />
                    </div>
                    <div className="capacity-text">
                      {capacity.allocatedHours.toFixed(1)} / {capacity.totalCapacity.toFixed(1)}
                    </div>
                  </div>
                );
              })
            )}
          </div>

          <div className="timeline-row projects-row">
            <div className="row-label">Projects</div>
            {weeks.map(week =>
              week.dates.map(date => {
                const projectsOnDate = getProjectsForDate(date);

                return (
                  <div
                    key={date.toISOString()}
                    className="project-cell"
                    onDragOver={handleDragOver}
                    onDrop={(e) => handleDrop(date, e)}
                  >
                    {projectsOnDate.map(({ project, allocation }) => (
                      <div
                        key={project.projectId}
                        className="project-bar"
                        title={`${project.projectNumber} - ${allocation.allocatedHours}h`}
                        draggable
                        onDragStart={(e) => handleDragStart(project, e)}
                      >
                        {project.projectNumber}
                      </div>
                    ))}
                  </div>
                );
              })
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default SquadGantt;
