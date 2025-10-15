import React, { useState, useEffect } from 'react';
import { getGanttData } from '../api';
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

  // Date range
  const [dateRange, setDateRange] = useState({ start: null, end: null });

  // Project edit modal
  const [editingProject, setEditingProject] = useState(null);

  useEffect(() => {
    fetchGanttData();
  }, []);

  const fetchGanttData = async (startDate = null, endDate = null) => {
    setLoading(true);
    setError(null);
    try {
      const response = await getGanttData(startDate, endDate, null);
      setGanttData(response.data);

      // Initialize squad filter with all squads selected
      const squads = response.data.squads.map(s => s.squadId);
      setAllSquads(response.data.squads);
      setSelectedSquads(squads);

      // Set date range
      setDateRange({
        start: new Date(response.data.dateRange.minDate),
        end: new Date(response.data.dateRange.maxDate)
      });
    } catch (err) {
      console.error('Error fetching Gantt data:', err);
      setError('Failed to load timeline data');
    } finally {
      setLoading(false);
    }
  };

  const handleZoomChange = (newZoom) => {
    setZoomLevel(newZoom);

    // Adjust date range based on zoom level
    const today = new Date();
    let start = today;
    let end;

    if (newZoom === 'day') {
      end = new Date(today);
      end.setMonth(end.getMonth() + 1); // 1 month
    } else if (newZoom === 'week') {
      end = new Date(today);
      end.setMonth(end.getMonth() + 3); // 3 months
    } else if (newZoom === 'month') {
      end = new Date(today);
      end.setFullYear(end.getFullYear() + 1); // 12 months
    }

    fetchGanttData(start.toISOString(), end.toISOString());
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

  const handleProjectClick = (projectId) => {
    // Find the project in the gantt data
    for (const squad of ganttData.squads) {
      const project = squad.projects.find(p => p.projectId === projectId);
      if (project) {
        setEditingProject(project);
        break;
      }
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
        <div className="control-group">
          <label>Squads:</label>
          <div className="squad-filter-buttons">
            <button className="btn btn-sm btn-outline" onClick={handleSelectAllSquads}>
              All
            </button>
            <button className="btn btn-sm btn-outline" onClick={handleDeselectAllSquads}>
              None
            </button>
          </div>
          <div className="squad-checkboxes">
            {allSquads.map(squad => (
              <label key={squad.squadId} className="checkbox-label">
                <input
                  type="checkbox"
                  checked={selectedSquads.includes(squad.squadId)}
                  onChange={() => handleSquadFilterChange(squad.squadId)}
                />
                {squad.squadName}
              </label>
            ))}
          </div>
        </div>

        {/* Date Range Display */}
        {dateRange.start && dateRange.end && (
          <div className="control-group date-range-display">
            <label>Date Range:</label>
            <span className="date-range-text">
              {dateRange.start.toLocaleDateString()} - {dateRange.end.toLocaleDateString()}
            </span>
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
