import React, { useState, useEffect } from 'react';
import { getGanttData, getProject } from '../api';
import GanttChart from '../components/GanttChart';
import ProjectForm from '../components/ProjectForm';
import './GanttPage.css';

function GanttPage() {
  const [ganttData, setGanttData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Zoom level: 'day', 'week', 'month'
  const [zoomLevel, setZoomLevel] = useState('week');

  // Squad filter
  const [selectedSquads, setSelectedSquads] = useState([]);
  const [allSquads, setAllSquads] = useState([]);
  const [showSquadDropdown, setShowSquadDropdown] = useState(false);

  // Date range
  const [dateRange, setDateRange] = useState({ start: null, end: null });

  // Project edit modal
  const [editingProject, setEditingProject] = useState(null);

  useEffect(() => {
    // Fetch with default date range starting from last week
    const { start, end } = getDefaultDateRange(zoomLevel);
    fetchGanttData(start.toISOString(), end.toISOString());
  }, []);

  // Close squad dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (showSquadDropdown && !event.target.closest('.squad-filter-dropdown')) {
        setShowSquadDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [showSquadDropdown]);

  const fetchGanttData = async (startDate = null, endDate = null, updateDateRange = true) => {
    setLoading(true);
    setError(null);
    try {
      const response = await getGanttData(startDate, endDate, null);
      setGanttData(response.data);

      // Initialize squad filter with all squads selected
      const squads = response.data.squads.map(s => s.squadId);
      setAllSquads(response.data.squads);
      setSelectedSquads(squads);

      // Only update date range if requested (not for manual user changes)
      if (updateDateRange) {
        setDateRange({
          start: new Date(response.data.dateRange.minDate),
          end: new Date(response.data.dateRange.maxDate)
        });
      }
    } catch (err) {
      console.error('Error fetching Gantt data:', err);
      setError('Failed to load timeline data');
    } finally {
      setLoading(false);
    }
  };

  const getDefaultDateRange = (zoomLevel) => {
    const today = new Date();

    // Start from one week (7 days) before today
    const start = new Date(today);
    start.setDate(start.getDate() - 7);

    let end;
    if (zoomLevel === 'day') {
      end = new Date(start);
      end.setMonth(end.getMonth() + 1); // 1 month from start
    } else if (zoomLevel === 'week') {
      end = new Date(start);
      end.setMonth(end.getMonth() + 3); // 3 months from start
    } else if (zoomLevel === 'month') {
      end = new Date(start);
      end.setFullYear(end.getFullYear() + 1); // 12 months from start
    }

    return { start, end };
  };

  const handleZoomChange = (newZoom) => {
    setZoomLevel(newZoom);
    // Keep the current date range instead of recalculating
    // Just fetch data with the existing date range
    if (dateRange.start && dateRange.end) {
      fetchGanttData(dateRange.start.toISOString(), dateRange.end.toISOString(), false);
    }
  };

  const handleDateRangeChange = (startDate, endDate) => {
    // Update the date range state immediately with user's selection
    setDateRange({
      start: new Date(startDate),
      end: new Date(endDate)
    });
    // Fetch data without overwriting the date range
    fetchGanttData(startDate, endDate, false);
  };

  const handleSquadFilterChange = (squadId) => {
    setSelectedSquads(prev => {
      if (prev.includes(squadId)) {
        return prev.filter(id => id !== squadId);
      } else {
        return [...prev, squadId];
      }
    });
  };

  const handleSelectAllSquads = () => {
    setSelectedSquads(allSquads.map(s => s.squadId));
  };

  const handleDeselectAllSquads = () => {
    setSelectedSquads([]);
  };

  const handleProjectClick = async (projectId) => {
    // Fetch the full project data from the API
    try {
      const response = await getProject(projectId);
      setEditingProject(response.data);
    } catch (error) {
      console.error('Error fetching project:', error);
      alert('Failed to load project details');
    }
  };

  const handleProjectSave = () => {
    setEditingProject(null);
    fetchGanttData(); // Refresh data
  };

  const handleProjectCancel = () => {
    setEditingProject(null);
  };

  // Filter squads based on selection
  const filteredSquads = ganttData?.squads.filter(s => selectedSquads.includes(s.squadId)) || [];

  return (
    <div className="gantt-page">
      <div className="gantt-page-header">
        <h1>Project Timeline</h1>
        <p className="subtitle">Visual timeline of all squad projects</p>
      </div>

      <div className="gantt-controls">
        {/* Zoom Controls */}
        <div className="control-group">
          <label>View:</label>
          <div className="btn-group">
            <button
              className={`btn btn-sm ${zoomLevel === 'day' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => handleZoomChange('day')}
            >
              Day
            </button>
            <button
              className={`btn btn-sm ${zoomLevel === 'week' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => handleZoomChange('week')}
            >
              Week
            </button>
            <button
              className={`btn btn-sm ${zoomLevel === 'month' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => handleZoomChange('month')}
            >
              Month
            </button>
          </div>
        </div>

        {/* Squad Filter */}
        <div className="control-group squad-filter-group">
          <label>Squads:</label>
          <div className="squad-filter-dropdown">
            <button
              className="btn btn-sm btn-outline squad-filter-btn"
              onClick={() => setShowSquadDropdown(!showSquadDropdown)}
            >
              {selectedSquads.length === allSquads.length
                ? 'All Squads'
                : selectedSquads.length === 0
                ? 'No Squads'
                : `${selectedSquads.length} Squad${selectedSquads.length > 1 ? 's' : ''}`}
              <span className="dropdown-arrow">{showSquadDropdown ? '▲' : '▼'}</span>
            </button>

            {showSquadDropdown && (
              <div className="squad-dropdown-menu">
                <div className="squad-dropdown-actions">
                  <button
                    className="btn btn-xs btn-link"
                    onClick={(e) => { e.stopPropagation(); handleSelectAllSquads(); }}
                  >
                    Select All
                  </button>
                  <button
                    className="btn btn-xs btn-link"
                    onClick={(e) => { e.stopPropagation(); handleDeselectAllSquads(); }}
                  >
                    Clear
                  </button>
                </div>
                <div className="squad-dropdown-options">
                  {allSquads.map(squad => (
                    <label key={squad.squadId} className="squad-dropdown-option">
                      <input
                        type="checkbox"
                        checked={selectedSquads.includes(squad.squadId)}
                        onChange={() => handleSquadFilterChange(squad.squadId)}
                      />
                      <span>{squad.squadName}</span>
                    </label>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Date Range Controls */}
        {dateRange.start && dateRange.end && (
          <div className="control-group date-range-controls">
            <label>Date Range:</label>
            <input
              type="date"
              className="date-input"
              value={dateRange.start.toISOString().split('T')[0]}
              onChange={(e) => {
                const newStart = new Date(e.target.value);
                handleDateRangeChange(newStart.toISOString(), dateRange.end.toISOString());
              }}
            />
            <span className="date-separator">to</span>
            <input
              type="date"
              className="date-input"
              value={dateRange.end.toISOString().split('T')[0]}
              onChange={(e) => {
                const newEnd = new Date(e.target.value);
                handleDateRangeChange(dateRange.start.toISOString(), newEnd.toISOString());
              }}
            />
          </div>
        )}
      </div>

      {loading && (
        <div className="loading-message">Loading timeline data...</div>
      )}

      {error && (
        <div className="error-message">{error}</div>
      )}

      {!loading && !error && filteredSquads.length === 0 && (
        <div className="empty-message">
          No squads selected or no projects found.
        </div>
      )}

      {!loading && !error && filteredSquads.length > 0 && (
        <GanttChart
          key={`${dateRange.start?.getTime()}-${dateRange.end?.getTime()}-${zoomLevel}`}
          squads={filteredSquads}
          dateRange={dateRange}
          zoomLevel={zoomLevel}
          onProjectClick={handleProjectClick}
        />
      )}

      {editingProject && (
        <ProjectForm
          project={editingProject}
          onSave={handleProjectSave}
          onCancel={handleProjectCancel}
        />
      )}
    </div>
  );
}

export default GanttPage;
