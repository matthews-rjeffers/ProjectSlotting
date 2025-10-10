import { useState, useEffect } from 'react';
import { getScheduleSuggestion, applyScheduleSuggestion, getSquads } from '../api';
import './ScheduleSuggestionModal.css';

const ScheduleSuggestionModal = ({ project, onClose, onSuccess }) => {
  const [squads, setSquads] = useState([]);
  const [selectedSquadId, setSelectedSquadId] = useState('');
  const [bufferPercentage, setBufferPercentage] = useState(project.bufferPercentage || 20);
  const [suggestion, setSuggestion] = useState(null);
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadSquads();
  }, []);

  const loadSquads = async () => {
    try {
      const response = await getSquads();
      setSquads(response.data);
      if (response.data.length > 0) {
        setSelectedSquadId(response.data[0].squadId);
      }
    } catch (error) {
      console.error('Error loading squads:', error);
      setError('Failed to load squads');
    }
  };

  const handleGetSuggestion = async () => {
    if (!selectedSquadId) {
      setError('Please select a squad');
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await getScheduleSuggestion(
        project.projectId,
        selectedSquadId,
        bufferPercentage
      );
      setSuggestion(response.data);
    } catch (error) {
      console.error('Error getting schedule suggestion:', error);
      setError('Failed to get schedule suggestion');
      setSuggestion(null);
    } finally {
      setLoading(false);
    }
  };

  const handleApplySuggestion = async () => {
    if (!suggestion || !suggestion.canAllocate) return;

    setApplying(true);
    setError('');

    try {
      await applyScheduleSuggestion(
        project.projectId,
        selectedSquadId,
        bufferPercentage
      );
      onSuccess();
      onClose();
    } catch (error) {
      console.error('Error applying schedule:', error);
      setError('Failed to apply schedule suggestion');
    } finally {
      setApplying(false);
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content schedule-suggestion-modal">
        <div className="modal-header">
          <h2>Schedule Suggestion for {project.projectNumber}</h2>
          <button className="close-button" onClick={onClose}>×</button>
        </div>

        <div className="modal-body">
          <div className="project-info">
            <h3>{project.customerName}</h3>
            <p>{project.customerCity}, {project.customerState}</p>
            <p>Estimated Dev Hours: <strong>{project.estimatedDevHours}h</strong></p>
          </div>

          <div className="suggestion-controls">
            <div className="control-group">
              <label htmlFor="squad-select">Select Squad</label>
              <select
                id="squad-select"
                value={selectedSquadId}
                onChange={(e) => setSelectedSquadId(e.target.value)}
                disabled={loading || applying}
              >
                {squads.map(squad => (
                  <option key={squad.squadId} value={squad.squadId}>
                    {squad.squadName} - {squad.squadLeadName}
                  </option>
                ))}
              </select>
            </div>

            <div className="control-group">
              <label htmlFor="buffer-input">
                Buffer Percentage: <strong>{bufferPercentage}%</strong>
              </label>
              <div className="buffer-input-group">
                <input
                  type="range"
                  id="buffer-input"
                  min="0"
                  max="50"
                  value={bufferPercentage}
                  onChange={(e) => setBufferPercentage(Number(e.target.value))}
                  disabled={loading || applying}
                />
                <input
                  type="number"
                  value={bufferPercentage}
                  min="0"
                  max="100"
                  onChange={(e) => setBufferPercentage(Number(e.target.value))}
                  disabled={loading || applying}
                  className="buffer-number-input"
                />
                <span>%</span>
              </div>
              <div className="buffer-calculation">
                Original: {project.estimatedDevHours}h + {bufferPercentage}% = {' '}
                <strong>{(project.estimatedDevHours * (1 + bufferPercentage / 100)).toFixed(1)}h</strong>
              </div>
            </div>

            <button
              className="btn btn-primary"
              onClick={handleGetSuggestion}
              disabled={loading || applying || !selectedSquadId}
            >
              {loading ? 'Calculating...' : 'Get Schedule Suggestion'}
            </button>
          </div>

          {error && (
            <div className="error-message">{error}</div>
          )}

          {suggestion && (
            <div className={`suggestion-result ${suggestion.canAllocate ? 'can-allocate' : 'cannot-allocate'}`}>
              {suggestion.canAllocate ? (
                <>
                  <h4>Suggested Schedule</h4>
                  <div className="schedule-details">
                    <div className="schedule-row">
                      <span className="schedule-label">Start Date:</span>
                      <span className="schedule-value">{formatDate(suggestion.suggestedStartDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Code Complete (CRP):</span>
                      <span className="schedule-value">{formatDate(suggestion.estimatedCrpDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">UAT Date:</span>
                      <span className="schedule-value">{formatDate(suggestion.estimatedUatDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Go-Live Date:</span>
                      <span className="schedule-value">{formatDate(suggestion.estimatedGoLiveDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Duration:</span>
                      <span className="schedule-value">{suggestion.estimatedDurationDays} working days</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Buffered Hours:</span>
                      <span className="schedule-value">{suggestion.bufferedDevHours.toFixed(1)}h</span>
                    </div>
                  </div>

                  <div className="onsite-preview">
                    <h5>Onsite Schedule (Will be created automatically)</h5>
                    <ul>
                      <li>UAT Week: 1 engineer for 1 week (40h)</li>
                      <li>Go-Live Week: 1 engineer for 1 week (40h)</li>
                    </ul>
                  </div>
                </>
              ) : (
                <div className="cannot-allocate-message">
                  <h4>Cannot Schedule</h4>
                  <p>{suggestion.message}</p>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button
            className="btn btn-secondary"
            onClick={onClose}
            disabled={applying}
          >
            Cancel
          </button>
          {suggestion && suggestion.canAllocate && (
            <button
              className="btn btn-success"
              onClick={handleApplySuggestion}
              disabled={applying}
            >
              {applying ? 'Applying...' : 'Apply Schedule'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default ScheduleSuggestionModal;