import { useState, useEffect } from 'react';
import { getSquadRecommendations, applyScheduleSuggestion } from '../api';
import './ScheduleSuggestionModal.css';

// Helper function to get next Monday
const getNextMonday = () => {
  const today = new Date();
  const dayOfWeek = today.getDay(); // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

  let daysUntilMonday;
  if (dayOfWeek === 1) {
    // Already Monday -> use next Monday (7 days from now)
    daysUntilMonday = 7;
  } else if (dayOfWeek === 0) {
    // Sunday -> tomorrow is Monday
    daysUntilMonday = 1;
  } else {
    // Tuesday (2) - Saturday (6) -> calculate days until next Monday
    daysUntilMonday = 8 - dayOfWeek;
  }

  const nextMonday = new Date(today);
  nextMonday.setDate(today.getDate() + daysUntilMonday);
  return nextMonday.toISOString().split('T')[0];
};

const ScheduleSuggestionModal = ({ project, onClose, onSuccess }) => {
  const [recommendations, setRecommendations] = useState([]);
  const [selectedRecommendation, setSelectedRecommendation] = useState(null);
  const [bufferPercentage, setBufferPercentage] = useState(project.bufferPercentage || 20);
  const [algorithmType, setAlgorithmType] = useState('strict'); // 'greedy', 'strict', or 'delayed'
  const [startDate, setStartDate] = useState(getNextMonday());
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadRecommendations();
  }, []);

  // Reload recommendations when parameters change
  useEffect(() => {
    if (recommendations.length > 0) {
      loadRecommendations();
    }
  }, [bufferPercentage, algorithmType, startDate]);

  const loadRecommendations = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await getSquadRecommendations(
        project.projectId,
        bufferPercentage,
        algorithmType,
        startDate
      );
      setRecommendations(response.data);
      // Auto-select top recommendation if it can allocate
      if (response.data.length > 0 && response.data[0].canAllocate) {
        setSelectedRecommendation(response.data[0]);
      } else if (response.data.length > 0) {
        setSelectedRecommendation(response.data[0]);
      }
    } catch (error) {
      console.error('Error loading squad recommendations:', error);
      setError('Failed to load squad recommendations');
    } finally {
      setLoading(false);
    }
  };

  const handleApplySuggestion = async () => {
    if (!selectedRecommendation || !selectedRecommendation.canAllocate) return;

    setApplying(true);
    setError('');

    try {
      await applyScheduleSuggestion(
        project.projectId,
        selectedRecommendation.squadId,
        bufferPercentage,
        algorithmType,
        startDate
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
              <label htmlFor="algorithm-select">Algorithm Type</label>
              <select
                id="algorithm-select"
                value={algorithmType}
                onChange={(e) => setAlgorithmType(e.target.value)}
                disabled={loading || applying}
              >
                <option value="greedy">Greedy Algorithm (Flexible - uses partial capacity)</option>
                <option value="strict">Strict Algorithm (Even distribution)</option>
                <option value="delayed">Delayed Algorithm (Reverse greedy - latest start)</option>
              </select>
            </div>

            <div className="control-group">
              <label htmlFor="start-date-input">Start Date</label>
              <input
                type="date"
                id="start-date-input"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                disabled={loading || applying}
              />
              <small className="field-hint">Defaults to next Monday</small>
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
          </div>

          {loading && (
            <div className="loading-message">Loading squad recommendations...</div>
          )}

          {!loading && recommendations.length > 0 && (
            <div className="squad-recommendations">
              <h4>Recommended Squads</h4>
              {recommendations.map((rec, index) => (
                <div
                  key={rec.squadId}
                  className={`squad-card ${selectedRecommendation?.squadId === rec.squadId ? 'selected' : ''} ${!rec.canAllocate ? 'unavailable' : ''}`}
                  onClick={() => rec.canAllocate && setSelectedRecommendation(rec)}
                  style={{ cursor: rec.canAllocate ? 'pointer' : 'not-allowed' }}
                >
                  <div className="squad-header">
                    <div className="squad-name-section">
                      <span className="squad-rank">#{index + 1}</span>
                      <span className="squad-name">{rec.squadName}</span>
                    </div>
                    <span className="squad-score" title={`Overall Score: ${rec.overallScore.toFixed(1)}/100`}>
                      {'★'.repeat(Math.round(rec.overallScore / 20))}{'☆'.repeat(5 - Math.round(rec.overallScore / 20))} {(rec.overallScore / 20).toFixed(1)}/5
                    </span>
                  </div>
                  <div className="squad-reason">{rec.recommendationReason}</div>
                  <div className="squad-scores">
                    <span className="score-badge" title="How well the squad fits the project size">
                      Capacity: {rec.capacityScore.toFixed(0)}
                    </span>
                    <span className="score-badge" title="Current workload level">
                      Workload: {rec.workloadScore.toFixed(0)}
                    </span>
                    <span className="score-badge" title="Number of active projects">
                      Projects: {rec.projectCountScore.toFixed(0)}
                    </span>
                    <span className="score-badge" title="Squad size match">
                      Size: {rec.sizeScore.toFixed(0)}
                    </span>
                  </div>
                  {!rec.canAllocate && rec.suggestion && (
                    <div className="squad-error">
                      Unable to allocate: {rec.suggestion.message}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}

          {error && (
            <div className="error-message">{error}</div>
          )}

          {selectedRecommendation && selectedRecommendation.suggestion && (
            <div className={`suggestion-result ${selectedRecommendation.canAllocate ? 'can-allocate' : 'cannot-allocate'}`}>
              {selectedRecommendation.canAllocate ? (
                <>
                  <h4>Suggested Schedule for {selectedRecommendation.squadName}</h4>
                  <div className="schedule-details">
                    <div className="schedule-row">
                      <span className="schedule-label">Start Date:</span>
                      <span className="schedule-value">{formatDate(selectedRecommendation.suggestion.suggestedStartDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Code Complete (CRP):</span>
                      <span className="schedule-value">{formatDate(selectedRecommendation.suggestion.estimatedCrpDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">UAT Date:</span>
                      <span className="schedule-value">{formatDate(selectedRecommendation.suggestion.estimatedUatDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Go-Live Date:</span>
                      <span className="schedule-value">{formatDate(selectedRecommendation.suggestion.estimatedGoLiveDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Duration:</span>
                      <span className="schedule-value">{selectedRecommendation.suggestion.estimatedDurationDays} working days</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">Buffered Hours:</span>
                      <span className="schedule-value">{selectedRecommendation.suggestion.bufferedDevHours.toFixed(1)}h</span>
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
                  <p>{selectedRecommendation.suggestion.message}</p>
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
          {selectedRecommendation && selectedRecommendation.canAllocate && (
            <button
              className="btn btn-success"
              onClick={handleApplySuggestion}
              disabled={applying}
            >
              {applying ? 'Applying...' : `Apply Schedule to ${selectedRecommendation.squadName}`}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default ScheduleSuggestionModal;