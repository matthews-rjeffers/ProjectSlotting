import React, { useMemo } from 'react';
import './GanttChart.css';

const PHASE_COLORS = {
  Development: '#4A90E2',
  UAT: '#F5A623',
  GoLive: '#BD10E0'
};

const MILESTONE_COLORS = {
  CRP: '#FFD700',
  UAT: '#F5A623',
  GoLive: '#7ED321'
};

function GanttChart({ squads, dateRange, zoomLevel, onProjectClick }) {
  // Generate timeline columns based on zoom level and date range
  const timelineColumns = useMemo(() => {
    if (!dateRange.start || !dateRange.end) return [];

    const columns = [];
    const start = new Date(dateRange.start);
    const end = new Date(dateRange.end);

    if (zoomLevel === 'day') {
      // Daily columns
      let current = new Date(start);
      while (current <= end) {
        columns.push({
          date: new Date(current),
          label: `${current.getMonth() + 1}/${current.getDate()}`
        });
        current.setDate(current.getDate() + 1);
      }
    } else if (zoomLevel === 'week') {
      // Weekly columns
      let current = new Date(start);
      // Start at beginning of week
      current.setDate(current.getDate() - current.getDay());

      while (current <= end) {
        const weekEnd = new Date(current);
        weekEnd.setDate(weekEnd.getDate() + 6);
        columns.push({
          date: new Date(current),
          label: `${current.getMonth() + 1}/${current.getDate()}`
        });
        current.setDate(current.getDate() + 7);
      }
    } else if (zoomLevel === 'month') {
      // Monthly columns
      let current = new Date(start.getFullYear(), start.getMonth(), 1);

      while (current <= end) {
        columns.push({
          date: new Date(current),
          label: current.toLocaleDateString('en-US', { month: 'short', year: '2-digit' })
        });
        current.setMonth(current.getMonth() + 1);
      }
    }

    return columns;
  }, [dateRange, zoomLevel]);

  // Calculate position and width for a phase/milestone
  const calculatePosition = (startDate, endDate = null) => {
    const timelineStart = dateRange.start.getTime();
    const timelineEnd = dateRange.end.getTime();
    const totalDuration = timelineEnd - timelineStart;

    const start = new Date(startDate).getTime();
    const left = ((start - timelineStart) / totalDuration) * 100;

    if (endDate) {
      const end = new Date(endDate).getTime();
      const width = ((end - start) / totalDuration) * 100;
      return { left: `${Math.max(0, left)}%`, width: `${Math.max(0.5, width)}%` };
    } else {
      // Milestone (point in time)
      return { left: `${Math.max(0, left)}%` };
    }
  };

  const formatDate = (date) => {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  return (
    <div className="gantt-chart">
      <div className="gantt-header">
        <div className="gantt-sidebar-header">Squad / Project</div>
        <div className="gantt-timeline-header">
          {timelineColumns.map((col, index) => (
            <div key={index} className="timeline-column-header">
              {col.label}
            </div>
          ))}
        </div>
      </div>

      <div className="gantt-body">
        {squads.map(squad => (
          <div key={squad.squadId} className="gantt-squad-section">
            <div className="gantt-squad-header">
              <div className="gantt-sidebar">
                <strong>{squad.squadName}</strong>
              </div>
              <div className="gantt-timeline">
                {/* Empty timeline for squad header */}
              </div>
            </div>

            {squad.projects.map(project => (
              <div key={project.projectId} className="gantt-project-row">
                <div className="gantt-sidebar">
                  <div className="project-info">
                    <div className="project-number">{project.projectNumber}</div>
                    <div className="project-customer">{project.customerName}</div>
                  </div>
                </div>

                <div
                  className="gantt-timeline"
                  onClick={() => onProjectClick(project.projectId)}
                  title="Click to edit project"
                >
                  {/* Development Phase */}
                  {project.developmentPhase && (
                    <div
                      className="phase-bar development-phase"
                      style={{
                        ...calculatePosition(
                          project.developmentPhase.startDate,
                          project.developmentPhase.endDate
                        ),
                        backgroundColor: PHASE_COLORS.Development
                      }}
                      title={`Development: ${formatDate(project.developmentPhase.startDate)} - ${formatDate(project.developmentPhase.endDate)}`}
                    >
                      <span className="phase-label">Dev</span>
                    </div>
                  )}

                  {/* Onsite Phases (UAT and Go-Live) */}
                  {project.onsitePhases.map((phase, index) => {
                    const weekEnd = new Date(phase.weekStartDate);
                    weekEnd.setDate(weekEnd.getDate() + 6); // 1 week duration

                    return (
                      <div
                        key={index}
                        className={`phase-bar onsite-phase ${phase.type.toLowerCase()}-phase`}
                        style={{
                          ...calculatePosition(phase.weekStartDate, weekEnd),
                          backgroundColor: PHASE_COLORS[phase.type]
                        }}
                        title={`${phase.type}: ${formatDate(phase.weekStartDate)} (${phase.engineerCount} eng)`}
                      >
                        <span className="phase-label">{phase.type}</span>
                      </div>
                    );
                  })}

                  {/* Milestones */}
                  {project.milestones.map((milestone, index) => (
                    <div
                      key={index}
                      className={`milestone ${milestone.type.toLowerCase()}-milestone`}
                      style={{
                        ...calculatePosition(milestone.date),
                        borderColor: MILESTONE_COLORS[milestone.type]
                      }}
                      title={`${milestone.type}: ${formatDate(milestone.date)}`}
                    >
                      <div className="milestone-diamond"></div>
                      <div className="milestone-label">{milestone.type}</div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        ))}
      </div>

      {/* Legend */}
      <div className="gantt-legend">
        <div className="legend-title">Legend:</div>
        <div className="legend-items">
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.Development }}></div>
            <span>Development</span>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.UAT }}></div>
            <span>UAT</span>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.GoLive }}></div>
            <span>Go-Live</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.CRP }}></div>
            <span>CRP Milestone</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.GoLive }}></div>
            <span>Go-Live Milestone</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default GanttChart;
