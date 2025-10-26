import React, { useMemo, useRef, useEffect } from 'react';
import './GanttChart.css';

const PHASE_COLORS = {
  Development: '#4A90E2',
  UAT: '#F5A623',
  GoLive: '#BD10E0',
  Training: '#27ae60',
  PLCTesting: '#e74c3c',
  IntegrationTesting: '#7f8c8d',
  Hypercare: '#FF6B9D',
  Custom: '#34495e'
};

const MILESTONE_COLORS = {
  CodeComplete: '#4A90E2',
  CRP: '#E67E22',
  UAT: '#F5A623',
  GoLive: '#7ED321',
  Training: '#27ae60',
  PLCTesting: '#e74c3c',
  IntegrationTesting: '#7f8c8d',
  Hypercare: '#FF6B9D',
  Custom: '#34495e'
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

  // Calculate position and width for a phase/milestone using pixel-based positioning
  // This ensures alignment with columns regardless of zoom level
  const calculatePosition = (startDate, endDate = null) => {
    if (timelineColumns.length === 0) return { left: '0px', width: '0px' };

    const startTime = new Date(startDate).getTime();
    const endTime = endDate ? new Date(endDate).getTime() : startTime;

    // Calculate pixel position based on column structure
    let leftPx = 0;
    let widthPx = 0;

    if (zoomLevel === 'day') {
      // Each column is 80px wide (from CSS)
      const COLUMN_WIDTH_PX = 80;
      const timelineStart = dateRange.start.getTime();
      const DAY_MS = 24 * 60 * 60 * 1000;

      const startDays = (startTime - timelineStart) / DAY_MS;
      const durationDays = endDate ? (endTime - startTime) / DAY_MS : 0;

      leftPx = startDays * COLUMN_WIDTH_PX;
      widthPx = endDate ? Math.max(durationDays * COLUMN_WIDTH_PX, 2) : 0;

    } else if (zoomLevel === 'week') {
      // Each column is 80px wide (from CSS)
      const COLUMN_WIDTH_PX = 80;
      // Use the first column's date (which is the Sunday before dateRange.start)
      const timelineStart = timelineColumns[0].date.getTime();
      const WEEK_MS = 7 * 24 * 60 * 60 * 1000;

      const startWeeks = (startTime - timelineStart) / WEEK_MS;
      const durationWeeks = endDate ? (endTime - startTime) / WEEK_MS : 0;

      leftPx = startWeeks * COLUMN_WIDTH_PX;
      widthPx = endDate ? Math.max(durationWeeks * COLUMN_WIDTH_PX, 2) : 0;

    } else if (zoomLevel === 'month') {
      // Months have variable widths based on widthPercent
      // Calculate total timeline width first
      const totalTimelineDuration = dateRange.end.getTime() - dateRange.start.getTime();

      // For month view, we need to calculate based on proportional position
      const startPercent = ((startTime - dateRange.start.getTime()) / totalTimelineDuration) * 100;
      const widthPercent = endDate ? ((endTime - startTime) / totalTimelineDuration) * 100 : 0;

      return {
        left: `${Math.max(0, startPercent)}%`,
        width: endDate ? `${Math.max(0.5, widthPercent)}%` : undefined
      };
    }

    if (endDate) {
      return {
        left: `${leftPx}px`,
        width: `${widthPx}px`
      };
    } else {
      // Milestone (point in time)
      return { left: `${leftPx}px` };
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
                    {/* Development Phase 1: Start → Code Complete */}
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

                    {/* Development Phase 2: CRP → UAT (Polish) */}
                    {project.polishPhase && (
                      <div
                        className="phase-bar polish-phase"
                        style={{
                          ...calculatePosition(
                            project.polishPhase.startDate,
                            project.polishPhase.endDate
                          ),
                          backgroundColor: PHASE_COLORS.Development,
                          opacity: 0.7  // Slightly transparent to distinguish from Phase 1
                        }}
                        title={`Polish Development: ${formatDate(project.polishPhase.startDate)} - ${formatDate(project.polishPhase.endDate)}`}
                      >
                        <span className="phase-label">Polish</span>
                      </div>
                    )}

                    {/* Onsite Phases (all types) */}
                    {project.onsitePhases.map((phase, index) => {
                      // Use the actual start and end dates from the backend
                      const phaseStart = new Date(phase.startDate);
                      const phaseEnd = new Date(phase.endDate);

                      // Display "PLC" instead of "PLCTesting" for the label
                      const displayLabel = phase.type === 'PLCTesting' ? 'PLC' : phase.type;

                      return (
                        <div
                          key={index}
                          className={`phase-bar onsite-phase ${phase.type.toLowerCase()}-phase`}
                          style={{
                            ...calculatePosition(phaseStart, phaseEnd),
                            backgroundColor: PHASE_COLORS[phase.type] || '#888'
                          }}
                          title={`${phase.type}: ${formatDate(phase.startDate)} - ${formatDate(phase.endDate)} (${phase.engineerCount} eng, ${phase.totalHours}h)`}
                        >
                          <span className="phase-label">{displayLabel}</span>
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
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.Training }}></div>
            <span>Training</span>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.PLCTesting }}></div>
            <span>PLC Testing</span>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.IntegrationTesting }}></div>
            <span>Integration Testing</span>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.Hypercare }}></div>
            <span>Hypercare</span>
          </div>
          <div className="legend-item">
            <div className="legend-color" style={{ backgroundColor: PHASE_COLORS.Custom }}></div>
            <span>Custom</span>
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
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.Training }}></div>
            <span>Training</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.PLCTesting }}></div>
            <span>PLC Testing</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.IntegrationTesting }}></div>
            <span>Integration Testing</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.Hypercare }}></div>
            <span>Hypercare</span>
          </div>
          <div className="legend-item">
            <div className="legend-milestone" style={{ borderColor: MILESTONE_COLORS.Custom }}></div>
            <span>Custom</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default GanttChart;
