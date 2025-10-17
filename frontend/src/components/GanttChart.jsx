import React, { useMemo, useRef, useEffect } from 'react';
import './GanttChart.css';

const PHASE_COLORS = {
  Development: '#4A90E2',
  UAT: '#F5A623',
  GoLive: '#BD10E0'
};

const MILESTONE_COLORS = {
  CodeComplete: '#4A90E2',
  CRP: '#FFD700',
  UAT: '#F5A623',
  GoLive: '#7ED321'
};

function GanttChart({ squads, dateRange, zoomLevel, onProjectClick }) {
  const headerRef = useRef(null);
  const bodyRef = useRef(null);
  const isSyncingRef = useRef(false);

  // Synchronize scroll between header and body
  useEffect(() => {
    const header = headerRef.current;
    const body = bodyRef.current;

    if (!header || !body) return;

    const syncHeaderScroll = () => {
      if (!isSyncingRef.current) {
        isSyncingRef.current = true;
        body.scrollLeft = header.scrollLeft;
        requestAnimationFrame(() => {
          isSyncingRef.current = false;
        });
      }
    };

    const syncBodyScroll = () => {
      if (!isSyncingRef.current) {
        isSyncingRef.current = true;
        header.scrollLeft = body.scrollLeft;
        requestAnimationFrame(() => {
          isSyncingRef.current = false;
        });
      }
    };

    header.addEventListener('scroll', syncHeaderScroll);
    body.addEventListener('scroll', syncBodyScroll);

    return () => {
      header.removeEventListener('scroll', syncHeaderScroll);
      body.removeEventListener('scroll', syncBodyScroll);
    };
  }, []);
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
      // Start at beginning of week (Sunday)
      current.setDate(current.getDate() - current.getDay());

      // Generate weeks - add one week past the end date to ensure full coverage
      const extendedEnd = new Date(end);
      extendedEnd.setDate(extendedEnd.getDate() + 7);

      while (current < extendedEnd) {
        columns.push({
          date: new Date(current),
          label: `${current.getMonth() + 1}/${current.getDate()}`
        });
        current.setDate(current.getDate() + 7);
      }
    } else if (zoomLevel === 'month') {
      // Monthly columns with proportional widths based on days in month
      let current = new Date(start.getFullYear(), start.getMonth(), 1);
      const totalDuration = end.getTime() - start.getTime();

      while (current <= end) {
        const monthStart = new Date(current);
        const monthEnd = new Date(current.getFullYear(), current.getMonth() + 1, 0); // Last day of month

        // Calculate this month's width as a percentage of the total timeline
        const monthDuration = Math.min(monthEnd.getTime(), end.getTime()) - Math.max(monthStart.getTime(), start.getTime());
        const widthPercent = (monthDuration / totalDuration) * 100;

        columns.push({
          date: new Date(current),
          label: current.toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
          widthPercent: widthPercent
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
    let left = ((start - timelineStart) / totalDuration) * 100;

    if (endDate) {
      const end = new Date(endDate).getTime();

      // Clamp start and end to timeline boundaries
      const clampedStart = Math.max(start, timelineStart);
      const clampedEnd = Math.min(end, timelineEnd);

      // Calculate position and width based on clamped values
      const clampedLeft = ((clampedStart - timelineStart) / totalDuration) * 100;
      const clampedWidth = ((clampedEnd - clampedStart) / totalDuration) * 100;

      return {
        left: `${Math.max(0, clampedLeft)}%`,
        width: `${Math.max(0.5, clampedWidth)}%`
      };
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
        <div className="gantt-timeline-header" ref={headerRef}>
          {timelineColumns.map((col, index) => (
            <div
              key={index}
              className="timeline-column-header"
              style={col.widthPercent ? { width: `${col.widthPercent}%`, minWidth: 'auto', flexGrow: 0, flexShrink: 0 } : {}}
            >
              {col.label}
            </div>
          ))}
        </div>
      </div>

      <div className="gantt-body" ref={bodyRef}>
        {squads.map(squad => (
          <div key={squad.squadId} className="gantt-squad-section">
            <div className="gantt-squad-header">
              <div className="gantt-sidebar">
                <strong>{squad.squadName}</strong>
              </div>
              <div className="gantt-timeline-container">
                {timelineColumns.map((col, index) => (
                  <div
                    key={index}
                    className="timeline-column-body"
                    style={col.widthPercent ? { width: `${col.widthPercent}%`, minWidth: 'auto', flexGrow: 0, flexShrink: 0 } : {}}
                  ></div>
                ))}
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

                <div className="gantt-timeline-container">
                  {timelineColumns.map((col, index) => (
                    <div
                      key={index}
                      className="timeline-column-body"
                      style={col.widthPercent ? { width: `${col.widthPercent}%`, minWidth: 'auto', flexGrow: 0, flexShrink: 0 } : {}}
                    ></div>
                  ))}

                  <div
                    className="gantt-timeline-overlay"
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
                      // Find the corresponding milestone date for this phase
                      const milestone = project.milestones.find(m => m.type === phase.type);
                      if (!milestone) return null;

                      // The phase starts on the milestone date and lasts 1 week
                      const phaseStart = new Date(milestone.date);
                      const phaseEnd = new Date(milestone.date);
                      phaseEnd.setDate(phaseEnd.getDate() + 6); // 1 week duration (7 days - 1)

                      return (
                        <div
                          key={index}
                          className={`phase-bar onsite-phase ${phase.type.toLowerCase()}-phase`}
                          style={{
                            ...calculatePosition(phaseStart, phaseEnd),
                            backgroundColor: PHASE_COLORS[phase.type]
                          }}
                          title={`${phase.type}: ${formatDate(milestone.date)} (${phase.engineerCount} eng)`}
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
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.CodeComplete }}></div>
            <span>Code Complete</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.CRP }}></div>
            <span>CRP</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.GoLive }}></div>
            <span>Go-Live</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default GanttChart;
