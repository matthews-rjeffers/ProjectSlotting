import { useState, useEffect } from 'react';
import { getSquadRecommendations, applyScheduleSuggestion, getAlgorithmComparison, checkConflicts } from '../api';
import ConflictWarningModal from './ConflictWarningModal';
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
  const [algorithmComparison, setAlgorithmComparison] = useState([]);
  const [showComparison, setShowComparison] = useState(false);
  const [comparingSquadId, setComparingSquadId] = useState(null);
  const [bufferPercentage, setBufferPercentage] = useState(project.bufferPercentage || 20);
  const [algorithmType, setAlgorithmType] = useState('strict'); // 'greedy', 'strict', or 'delayed'
  const [startDate, setStartDate] = useState(getNextMonday());
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState('');
  const [conflicts, setConflicts] = useState(null);
  const [showConflictModal, setShowConflictModal] = useState(false);

  useEffect(() => {
    // Load squad recommendations on mount
    loadRecommendations();
  }, []);

  // Reload recommendations when parameters change
  useEffect(() => {
    if (recommendations.length > 0) {
      loadRecommendations();
    }
  }, [bufferPercentage, startDate, algorithmType]);

  const loadAlgorithmComparison = async (squadId) => {
    setLoading(true);
    setError('');
    try {
      const comparisonResponse = await getAlgorithmComparison(
        project.projectId,
        squadId,
        bufferPercentage,
        startDate
      );

      setAlgorithmComparison(comparisonResponse.data);
      setComparingSquadId(squadId);
      setShowComparison(true);
    } catch (error) {
      console.error('Error loading algorithm comparison:', error);
      setError('Failed to load algorithm comparison');
    } finally {
      setLoading(false);
    }
  };

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

  const handleSelectAlgorithm = (algoType) => {
    setAlgorithmType(algoType);
    setShowComparison(false);
    setAlgorithmComparison([]);
    setComparingSquadId(null);
    // Reload recommendations with the new algorithm
    loadRecommendations();
  };

  const handleApplySuggestion = async () => {
    if (!selectedRecommendation || !selectedRecommendation.canAllocate) return;

    // Check for conflicts first
    setApplying(true);
    setError('');

    try {
      const conflictResult = await checkConflicts(project.projectId, {
        squadId: selectedRecommendation.squadId,
        checkScheduleSuggestion: true,
        suggestion: selectedRecommendation.suggestion
      });

      if (conflictResult.data.hasConflicts && conflictResult.data.requiresConfirmation) {
        // Show conflict modal and wait for user confirmation
        setConflicts(conflictResult.data.conflicts);
        setShowConflictModal(true);
        setApplying(false);
        return;
      }

      // No conflicts or conflicts don't require confirmation, proceed
      await applyScheduleSuggestionActual();
    } catch (error) {
      console.error('Error checking conflicts or applying schedule:', error);
      setError('Failed to check conflicts or apply schedule');
      setApplying(false);
    }
  };

  const applyScheduleSuggestionActual = async () => {
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

  const handleConflictConfirm = async () => {
    setShowConflictModal(false);
    await applyScheduleSuggestionActual();
  };

  const handleConflictCancel = () => {
    setShowConflictModal(false);
    setConflicts(null);
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

          {loading && (
            <div className="loading-message">Loading algorithm comparison...</div>
          )}

          {showComparison && !loading && algorithmComparison.length > 0 && (
            <div className="algorithm-comparison-section">
              <div className="comparison-header">
                <h4>Compare Scheduling Algorithms</h4>
                <button
                  className="btn btn-sm btn-secondary"
                  onClick={() => {
                    setShowComparison(false);
                    setAlgorithmComparison([]);
                    setComparingSquadId(null);
                  }}
                >
                  ← Back to Squads
                </button>
              </div>
              <p className="comparison-hint">
                Comparing algorithms for {recommendations.find(r => r.squadId === comparingSquadId)?.squadName || 'selected squad'}.
                Select the algorithm that best fits your needs:
              </p>
              <div className="algorithm-cards">
                {algorithmComparison.map((algo) => (
                  <div
                    key={algo.algorithmType}
                    className={`algorithm-card ${!algo.canAllocate ? 'unavailable' : ''}`}
                    onClick={() => algo.canAllocate && handleSelectAlgorithm(algo.algorithmType)}
                    style={{ cursor: algo.canAllocate ? 'pointer' : 'not-allowed' }}
                  >
                    <div className="algorithm-header">
                      <h5>{algo.algorithmName}</h5>
                      {algo.canAllocate ? (
                        <span className="algorithm-status available">✓ Available</span>
                      ) : (
                        <span className="algorithm-status unavailable">✗ Unavailable</span>
                      )}
                    </div>
                    <p className="algorithm-description">{algo.description}</p>

                    {algo.canAllocate ? (
                      <>
                        <div className="algorithm-dates">
                          <div className="date-item">
                            <span className="date-label">Start:</span>
                            <span className="date-value">{formatDate(algo.suggestedStartDate)}</span>
                          </div>
                          <div className="date-item">
                            <span className="date-label">Code Complete:</span>
                            <span className="date-value">{formatDate(algo.estimatedCodeCompleteDate)}</span>
                          </div>
                          <div className="date-item">
                            <span className="date-label">CRP:</span>
                            <span className="date-value">{formatDate(algo.estimatedCrpDate)}</span>
                          </div>
                          <div className="date-item">
                            <span className="date-label">Duration:</span>
                            <span className="date-value">{algo.estimatedDurationDays} days</span>
                          </div>
                        </div>
                        <div className="algorithm-pros-cons">
                          <div className="pros">
                            <strong>✓</strong> {algo.pros}
                          </div>
                          <div className="cons">
                            <strong>✗</strong> {algo.cons}
                          </div>
                        </div>
                      </>
                    ) : (
                      <div className="algorithm-error">
                        {algo.message}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {!showComparison && (
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
          )}

          {!showComparison && loading && (
            <div className="loading-message">Loading squad recommendations...</div>
          )}

          {!showComparison && !loading && recommendations.length > 0 && (
            <div className="squad-recommendations">
              <h4>Recommended Squads</h4>
              {recommendations.map((rec, index) => (
                <div
                  key={rec.squadId}
                  className={`squad-card ${selectedRecommendation?.squadId === rec.squadId ? 'selected' : ''} ${!rec.canAllocate ? 'unavailable' : ''}`}
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
                  <div className="squad-actions">
                    <button
                      className="btn btn-sm btn-secondary"
                      onClick={(e) => {
                        e.stopPropagation();
                        loadAlgorithmComparison(rec.squadId);
                      }}
                      disabled={loading}
                    >
                      Compare Algorithms
                    </button>
                    <button
                      className="btn btn-sm btn-primary"
                      onClick={(e) => {
                        e.stopPropagation();
                        setSelectedRecommendation(rec);
                      }}
                      disabled={!rec.canAllocate}
                    >
                      Select Squad
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {!showComparison && error && (
            <div className="error-message">{error}</div>
          )}

          {!showComparison && selectedRecommendation && selectedRecommendation.suggestion && (
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
                      <span className="schedule-label">Code Complete Date:</span>
                      <span className="schedule-value">{formatDate(selectedRecommendation.suggestion.estimatedCodeCompleteDate)}</span>
                    </div>
                    <div className="schedule-row">
                      <span className="schedule-label">CRP Date:</span>
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

                  <div className="onsite-note">
                    <p><strong>Note:</strong> Onsite schedules (UAT, Go-Live, Hypercare, etc.) must be manually created after applying this schedule.</p>
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

      {showConflictModal && conflicts && (
        <ConflictWarningModal
          conflicts={conflicts}
          onConfirm={handleConflictConfirm}
          onCancel={handleConflictCancel}
        />
      )}
    </div>
  );
};

export default ScheduleSuggestionModal;