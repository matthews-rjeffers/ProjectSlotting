import React from 'react';
import './ConflictWarningModal.css';

function ConflictWarningModal({ conflicts, onConfirm, onCancel }) {
  if (!conflicts || conflicts.length === 0) return null;

  const criticalConflicts = conflicts.filter(c => c.severity === 'Critical');
  const warningConflicts = conflicts.filter(c => c.severity === 'Warning');

  return (
    <div className="conflict-modal-overlay">
      <div className="conflict-modal-content">
        <div className="conflict-modal-header">
          <h2>⚠️ Allocation Conflicts Detected</h2>
        </div>

        <div className="conflict-modal-body">
          <p className="conflict-summary">
            Found <strong>{criticalConflicts.length} critical issue(s)</strong> and{' '}
            <strong>{warningConflicts.length} warning(s)</strong>.
          </p>

          <div className="conflicts-list">
            {criticalConflicts.map((conflict, index) => (
              <div key={`critical-${index}`} className="conflict-item critical">
                <div className="conflict-icon">🔴</div>
                <div className="conflict-content">
                  <div className="conflict-title">{conflict.message}</div>
                  <div className="conflict-details">{conflict.details}</div>
                  {conflict.conflictDate && (
                    <div className="conflict-date">
                      First occurs: {new Date(conflict.conflictDate).toLocaleDateString()}
                    </div>
                  )}
                </div>
              </div>
            ))}

            {warningConflicts.map((conflict, index) => (
              <div key={`warning-${index}`} className="conflict-item warning">
                <div className="conflict-icon">⚠️</div>
                <div className="conflict-content">
                  <div className="conflict-title">{conflict.message}</div>
                  <div className="conflict-details">{conflict.details}</div>
                  {conflict.conflictingProjects && conflict.conflictingProjects.length > 0 && (
                    <div className="conflicting-projects">
                      <strong>Conflicting projects:</strong>
                      <ul>
                        {conflict.conflictingProjects.map((proj, idx) => (
                          <li key={idx}>{proj}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>

          <div className="conflict-confirmation">
            <p>
              <strong>Are you sure you want to proceed with this allocation?</strong>
            </p>
            <p className="conflict-note">
              You can override these warnings, but please review the conflicts carefully.
            </p>
          </div>
        </div>

        <div className="conflict-modal-footer">
          <button className="btn btn-cancel" onClick={onCancel}>
            Cancel
          </button>
          <button className="btn btn-proceed" onClick={onConfirm}>
            Proceed Anyway
          </button>
        </div>
      </div>
    </div>
  );
}

export default ConflictWarningModal;
