import React from 'react';
import './AllocationPreviewModal.css';

const AllocationPreviewModal = ({ isOpen, onClose, preview, projectName, onConfirm }) => {
  if (!isOpen || !preview) return null;

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  };

  return (
    <div className="preview-modal-overlay" onClick={onClose}>
      <div className="preview-modal-content" onClick={e => e.stopPropagation()}>
        <div className="preview-modal-header">
          <h2>Allocation Preview: {projectName}</h2>
          <button className="preview-modal-close" onClick={onClose}>&times;</button>
        </div>

        <div className="preview-modal-body">
          <div className={`preview-status ${preview.canAllocate ? 'success' : 'error'}`}>
            <div className="status-icon">{preview.canAllocate ? '✓' : '⚠'}</div>
            <div className="status-message">{preview.message}</div>
          </div>

          <div className="preview-table-container">
            <h3>Weekly Capacity Impact</h3>
            <table className="preview-table">
              <thead>
                <tr>
                  <th>Week</th>
                  <th>Capacity</th>
                  <th>Current</th>
                  <th>Preview</th>
                  <th>Current %</th>
                  <th>Preview %</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {preview.weeklyCapacity.map((week, idx) => (
                  <tr key={idx} className={week.wouldExceedCapacity ? 'over-capacity' : ''}>
                    <td className="week-range">
                      {formatDate(week.weekStart)} - {formatDate(week.weekEnd)}
                    </td>
                    <td className="capacity-value">{week.totalCapacity.toFixed(1)}h</td>
                    <td className="hours-breakdown">
                      <div className="hours-detail">
                        {week.currentDevHours > 0 && (
                          <span className="dev-hours">{week.currentDevHours.toFixed(1)}h dev</span>
                        )}
                        {week.currentOnsiteHours > 0 && (
                          <span className="onsite-hours">{week.currentOnsiteHours.toFixed(1)}h onsite</span>
                        )}
                        {week.currentDevHours === 0 && week.currentOnsiteHours === 0 && (
                          <span className="no-hours">0h</span>
                        )}
                      </div>
                      <div className="total-hours">
                        Total: {(week.currentDevHours + week.currentOnsiteHours).toFixed(1)}h
                      </div>
                    </td>
                    <td className="hours-breakdown">
                      <div className="hours-detail">
                        {(week.currentDevHours + week.previewDevHours) > 0 && (
                          <span className="dev-hours">
                            {(week.currentDevHours + week.previewDevHours).toFixed(1)}h dev
                            {week.previewDevHours > 0 && (
                              <span className="hours-added"> (+{week.previewDevHours.toFixed(1)})</span>
                            )}
                          </span>
                        )}
                        {(week.currentOnsiteHours + week.previewOnsiteHours) > 0 && (
                          <span className="onsite-hours">
                            {(week.currentOnsiteHours + week.previewOnsiteHours).toFixed(1)}h onsite
                            {week.previewOnsiteHours > 0 && (
                              <span className="hours-added"> (+{week.previewOnsiteHours.toFixed(1)})</span>
                            )}
                          </span>
                        )}
                      </div>
                      <div className="total-hours">
                        Total: {(week.currentDevHours + week.currentOnsiteHours + week.previewDevHours + week.previewOnsiteHours).toFixed(1)}h
                      </div>
                    </td>
                    <td className="utilization-cell">
                      <div className="utilization-bar">
                        <div
                          className="utilization-fill current"
                          style={{ width: `${Math.min(week.currentUtilization, 100)}%` }}
                        />
                      </div>
                      <span className="utilization-percent">{week.currentUtilization.toFixed(0)}%</span>
                    </td>
                    <td className="utilization-cell">
                      <div className="utilization-bar">
                        <div
                          className={`utilization-fill preview ${week.wouldExceedCapacity ? 'over' : ''}`}
                          style={{ width: `${Math.min(week.previewUtilization, 100)}%` }}
                        />
                      </div>
                      <span className={`utilization-percent ${week.wouldExceedCapacity ? 'over' : ''}`}>
                        {week.previewUtilization.toFixed(0)}%
                      </span>
                    </td>
                    <td className="status-cell">
                      {week.wouldExceedCapacity ? (
                        <span className="status-badge over">Over Capacity</span>
                      ) : week.previewUtilization >= 90 ? (
                        <span className="status-badge warning">Near Capacity</span>
                      ) : (
                        <span className="status-badge ok">OK</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="preview-modal-footer">
          <button className="btn-cancel" onClick={onClose}>Cancel</button>
          {preview.canAllocate && (
            <button className="btn-confirm" onClick={onConfirm}>
              Allocate Project
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default AllocationPreviewModal;
