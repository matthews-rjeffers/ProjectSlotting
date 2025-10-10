import React, { useState, useEffect } from 'react';
import { getSquadCapacity, allocateProject, reallocateProject, unassignProject, getProjectAllocations } from '../api';
import ScheduleSuggestionModal from './ScheduleSuggestionModal';
import './ImprovedCapacityView.css';

function ImprovedCapacityView({ squadId, squad, projects, dateRange, onProjectUpdate }) {
  const [capacityData, setCapacityData] = useState({});
  const [projectAllocations, setProjectAllocations] = useState({});
  const [weeklyData, setWeeklyData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedWeek, setSelectedWeek] = useState(null);
  const [showUnallocated, setShowUnallocated] = useState(true);
  const [scheduleSuggestionProject, setScheduleSuggestionProject] = useState(null);

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

      console.log('Capacity Response:', capacityResponse.data);
      setCapacityData(capacityResponse.data);

      // Load allocations for all projects (for this squad)
      const squadAllocationsMap = {};
      const allAllocationsMap = {}; // Track all allocations to any squad

      for (const project of projects) {
        try {
          const allocResponse = await getProjectAllocations(project.projectId);
          const allAllocations = allocResponse.data;

          // Track if project is allocated to ANY squad
          if (allAllocations.length > 0) {
            allAllocationsMap[project.projectId] = allAllocations;
          }

          // Track allocations for THIS squad specifically
          const squadAllocations = allAllocations.filter(a => a.squadId === squadId);
          if (squadAllocations.length > 0) {
            squadAllocationsMap[project.projectId] = squadAllocations;
          }
        } catch (error) {
          console.error(`Error loading allocations for project ${project.projectId}:`, error);
        }
      }

      // Store both - squad allocations for display, all allocations for filtering
      setProjectAllocations({
        squad: squadAllocationsMap,
        all: allAllocationsMap
      });

      // Calculate weekly data using squad-specific allocations
      calculateWeeklyData(capacityResponse.data, squadAllocationsMap);
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const calculateWeeklyData = (capacity, allocations) => {
    const weeks = getWeeksInRange();
    const weeklyAggregated = weeks.map(week => {
      let totalCapacity = 0;
      let totalAllocated = 0;
      let projectsThisWeek = new Set();

      let totalDevHours = 0;
      let totalUATHours = 0;
      let totalGoLiveHours = 0;

      week.dates.forEach(date => {
        const dateKey = date.toISOString().split('T')[0];
        const dayCapacity = capacity[dateKey] || {};

        // Calculate daily capacity from team members if not in API response
        const dailyTeamCapacity = squad?.teamMembers
          ?.filter(tm => tm.isActive)
          .reduce((sum, tm) => sum + (tm.dailyCapacityHours || 0), 0) || 0;

        totalCapacity += dayCapacity.totalCapacity || dailyTeamCapacity;

        // Calculate allocated hours for this date from allocations, separating by type
        let dayAllocatedHours = 0;
        Object.entries(allocations).forEach(([projectId, allocs]) => {
          const allocation = allocs.find(a => a.allocationDate.split('T')[0] === dateKey);
          if (allocation) {
            const hours = allocation.allocatedHours || 0;
            dayAllocatedHours += hours;

            // Track Development vs UAT vs GoLive hours
            const allocType = allocation.allocationType;
            if (allocType === 'UAT') {
              totalUATHours += hours;
            } else if (allocType === 'GoLive') {
              totalGoLiveHours += hours;
            } else {
              // Development or legacy types
              totalDevHours += hours;
            }

            projectsThisWeek.add(parseInt(projectId));
          }
        });

        // Use calculated allocated hours (more reliable than API response)
        totalAllocated += dayAllocatedHours;
      });

      const utilizationPercent = totalCapacity > 0 ? (totalAllocated / totalCapacity) * 100 : 0;
      const devPercent = totalCapacity > 0 ? (totalDevHours / totalCapacity) * 100 : 0;
      const uatPercent = totalCapacity > 0 ? (totalUATHours / totalCapacity) * 100 : 0;
      const goLivePercent = totalCapacity > 0 ? (totalGoLiveHours / totalCapacity) * 100 : 0;

      return {
        weekStart: week.start,
        weekEnd: week.end,
        dates: week.dates,
        totalCapacity: totalCapacity.toFixed(1),
        totalAllocated: totalAllocated.toFixed(1),
        totalDevHours: totalDevHours.toFixed(1),
        totalUATHours: totalUATHours.toFixed(1),
        totalGoLiveHours: totalGoLiveHours.toFixed(1),
        devPercent: devPercent.toFixed(1),
        uatPercent: uatPercent.toFixed(1),
        goLivePercent: goLivePercent.toFixed(1),
        remaining: (totalCapacity - totalAllocated).toFixed(1),
        utilizationPercent: utilizationPercent.toFixed(1),
        status: utilizationPercent > 100 ? 'overloaded' : utilizationPercent > 80 ? 'high' : 'normal',
        projectCount: projectsThisWeek.size,
        projects: Array.from(projectsThisWeek).map(id => projects.find(p => p.projectId === id)).filter(Boolean)
      };
    });

    setWeeklyData(weeklyAggregated);
  };

  const getWeeksInRange = () => {
    const start = new Date(dateRange.startDate);
    const end = new Date(dateRange.endDate);
    const weeks = [];

    let current = new Date(start);

    while (current <= end) {
      const weekStart = getWeekStart(current);
      const weekEnd = new Date(weekStart);
      weekEnd.setDate(weekEnd.getDate() + 4); // Friday

      const datesInWeek = [];
      for (let i = 0; i < 5; i++) {
        const date = new Date(weekStart);
        date.setDate(date.getDate() + i);
        if (date >= start && date <= end) {
          datesInWeek.push(date);
        }
      }

      if (datesInWeek.length > 0) {
        weeks.push({
          start: weekStart,
          end: weekEnd,
          dates: datesInWeek
        });
      }

      current.setDate(current.getDate() + 7);
    }

    return weeks;
  };

  const getWeekStart = (date) => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
  };

  const formatDate = (date) => {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  const formatWeekRange = (start, end) => {
    return `${formatDate(start)} - ${formatDate(end)}`;
  };

  const handleAllocateProject = async (project) => {
    const startDate = project.startDate || new Date().toISOString().split('T')[0];

    try {
      await allocateProject(project.projectId, squadId, startDate);
      await loadData();
      onProjectUpdate();
    } catch (error) {
      console.error('Error allocating project:', error);
      alert('Failed to allocate project. There may not be enough capacity.');
    }
  };

  const handleUnassignProject = async (projectId) => {
    if (!confirm('Are you sure you want to unassign this project? This will remove all allocations.')) {
      return;
    }

    try {
      await unassignProject(projectId);
      await loadData();
      onProjectUpdate();
    } catch (error) {
      console.error('Error unassigning project:', error);
      alert('Failed to unassign project.');
    }
  };

  if (loading) {
    return <div className="capacity-loading">Loading capacity data...</div>;
  }

  // Filter out projects allocated to ANY squad (not just current squad)
  const unallocatedProjects = projects.filter(p => !projectAllocations?.all || !projectAllocations.all[p.projectId]);

  const totalDailyCapacity = squad?.teamMembers?.filter(tm => tm.isActive).reduce((sum, tm) => sum + tm.dailyCapacityHours, 0) || 0;
  const teamSize = squad?.teamMembers?.filter(tm => tm.isActive).length || 0;

  // Calculate overall metrics
  const totalWeeklyCapacity = weeklyData.reduce((sum, week) => sum + parseFloat(week.totalCapacity), 0);
  const totalWeeklyAllocated = weeklyData.reduce((sum, week) => sum + parseFloat(week.totalAllocated), 0);
  const overallUtilization = totalWeeklyCapacity > 0 ? ((totalWeeklyAllocated / totalWeeklyCapacity) * 100).toFixed(1) : 0;

  // Count only projects allocated to THIS squad
  const allocatedProjectCount = projectAllocations?.squad ? Object.keys(projectAllocations.squad).length : 0;

  return (
    <div className="improved-capacity-view">
      {/* Summary Cards */}
      <div className="capacity-summary">
        <div className="summary-card">
          <div className="card-label">Team Size</div>
          <div className="card-value">{teamSize}</div>
          <div className="card-sublabel">active members</div>
        </div>
        <div className="summary-card">
          <div className="card-label">Daily Capacity</div>
          <div className="card-value">{totalDailyCapacity.toFixed(1)}</div>
          <div className="card-sublabel">hours per day</div>
        </div>
        <div className="summary-card">
          <div className="card-label">Overall Utilization</div>
          <div className="card-value">{overallUtilization}%</div>
          <div className="card-sublabel">across all weeks</div>
        </div>
        <div className="summary-card">
          <div className="card-label">Active Projects</div>
          <div className="card-value">{allocatedProjectCount}</div>
          <div className="card-sublabel">currently allocated</div>
        </div>
      </div>

      {/* Unallocated Projects */}
      {unallocatedProjects.length > 0 && (
        <div className="unallocated-section">
          <div className="section-header" onClick={() => setShowUnallocated(!showUnallocated)}>
            <h3>
              Unallocated Projects ({unallocatedProjects.length})
              <span className="toggle-icon">{showUnallocated ? '▼' : '▶'}</span>
            </h3>
          </div>
          {showUnallocated && (
            <div className="unallocated-grid">
              {unallocatedProjects.map(project => (
                <div key={project.projectId} className="unallocated-card">
                  <div className="project-header">
                    <strong>{project.projectNumber}</strong>
                    <span className="hours-badge">{project.estimatedDevHours}h</span>
                  </div>
                  <div className="project-customer">{project.customerName}</div>
                  {project.crpdate ? (
                    <>
                      <div className="project-dates">
                        CRP: {new Date(project.crpdate).toLocaleDateString()}
                      </div>
                      <button
                        onClick={() => handleAllocateProject(project)}
                        className="btn btn-small btn-primary"
                      >
                        Allocate to {squad?.squadName}
                      </button>
                    </>
                  ) : (
                    <>
                      <div className="project-dates">
                        <em>No dates set</em>
                      </div>
                      <button
                        onClick={() => setScheduleSuggestionProject(project)}
                        className="btn btn-small btn-success"
                      >
                        Suggest Schedule
                      </button>
                    </>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Weekly Capacity View */}
      <div className="weekly-capacity">
        <h3>Weekly Capacity Overview</h3>
        <div className="weeks-grid">
          {weeklyData.map((week, index) => (
            <div
              key={week.weekStart.toISOString()}
              className={`week-card ${week.status} ${selectedWeek === index ? 'selected' : ''}`}
              onClick={() => setSelectedWeek(selectedWeek === index ? null : index)}
            >
              <div className="week-header">
                <div className="week-date">{formatWeekRange(week.weekStart, week.weekEnd)}</div>
                <div className={`utilization-badge ${week.status}`}>
                  {week.utilizationPercent}%
                </div>
              </div>

              <div className="capacity-progress">
                <div className="progress-bar">
                  {/* Dev hours - Blue */}
                  {parseFloat(week.devPercent) > 0 && (
                    <div
                      className="progress-fill development"
                      style={{ width: `${Math.min(parseFloat(week.devPercent), 100)}%` }}
                      title={`Development: ${week.totalDevHours}h`}
                    />
                  )}
                  {/* UAT hours - Orange/Amber */}
                  {parseFloat(week.uatPercent) > 0 && (
                    <div
                      className="progress-fill uat"
                      style={{ width: `${Math.min(parseFloat(week.uatPercent), 100)}%` }}
                      title={`UAT: ${week.totalUATHours}h`}
                    />
                  )}
                  {/* GoLive hours - Red */}
                  {parseFloat(week.goLivePercent) > 0 && (
                    <div
                      className="progress-fill golive"
                      style={{ width: `${Math.min(parseFloat(week.goLivePercent), 100)}%` }}
                      title={`Go Live: ${week.totalGoLiveHours}h`}
                    />
                  )}
                </div>
                <div className="progress-text">
                  {parseFloat(week.totalDevHours) > 0 && (
                    <span className="dev-label">Dev: {week.totalDevHours}h</span>
                  )}
                  {parseFloat(week.totalUATHours) > 0 && (
                    <span className="uat-label"> | UAT: {week.totalUATHours}h</span>
                  )}
                  {parseFloat(week.totalGoLiveHours) > 0 && (
                    <span className="golive-label"> | Go-Live: {week.totalGoLiveHours}h</span>
                  )}
                  <span> / {week.totalCapacity}h</span>
                </div>
              </div>

              <div className="week-stats">
                <div className="stat">
                  <span className="stat-label">Available:</span>
                  <span className="stat-value">{week.remaining}h</span>
                </div>
                <div className="stat">
                  <span className="stat-label">Projects:</span>
                  <span className="stat-value">{week.projectCount}</span>
                </div>
              </div>

              {selectedWeek === index && week.projects.length > 0 && (
                <div className="week-projects">
                  <div className="projects-list">
                    {week.projects.map(project => (
                      <div key={project.projectId} className="project-item-card">
                        <div className="project-info-row">
                          <span className="project-number">{project.projectNumber}</span>
                          <span className="project-customer">{project.customerName}</span>
                        </div>
                        <div className="project-actions-row">
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              handleUnassignProject(project.projectId);
                            }}
                            className="btn btn-small btn-danger"
                            title="Unassign project from squad"
                          >
                            Unassign
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      {weeklyData.length === 0 && (
        <div className="empty-state">
          <p>No capacity data available for the selected date range.</p>
          <p>Try adjusting your date range or add team members to the squad.</p>
        </div>
      )}

      {/* Schedule Suggestion Modal */}
      {scheduleSuggestionProject && (
        <ScheduleSuggestionModal
          project={scheduleSuggestionProject}
          onClose={() => setScheduleSuggestionProject(null)}
          onSuccess={() => {
            setScheduleSuggestionProject(null);
            onProjectUpdate();
            loadData();
          }}
        />
      )}
    </div>
  );
}

export default ImprovedCapacityView;
