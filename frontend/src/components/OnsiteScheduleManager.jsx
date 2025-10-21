import React, { useState, useEffect } from 'react';
import { getProjectOnsiteSchedules, createOnsiteSchedule, updateOnsiteSchedule, deleteOnsiteSchedule } from '../api';
import './OnsiteScheduleManager.css';

const OnsiteScheduleManager = ({ projectId, uatDate, goLiveDate }) => {
  const [schedules, setSchedules] = useState([]);
  const [editingSchedule, setEditingSchedule] = useState(null);
  const [newSchedule, setNewSchedule] = useState({
    startDate: '',
    endDate: '',
    engineerCount: 1,
    totalHours: 40,
    onsiteType: 'UAT',
    notes: ''
  });

  useEffect(() => {
    if (projectId) {
      loadSchedules();
    }
  }, [projectId]);

  const loadSchedules = async () => {
    try {
      const response = await getProjectOnsiteSchedules(projectId);
      setSchedules(response.data);
    } catch (error) {
      console.error('Error loading schedules:', error);
    }
  };

  const handleAddSchedule = async () => {
    if (!newSchedule.startDate || !newSchedule.endDate) {
      alert('Please select both start and end dates');
      return;
    }

    if (new Date(newSchedule.endDate) < new Date(newSchedule.startDate)) {
      alert('End date must be on or after start date');
      return;
    }

    try {
      await createOnsiteSchedule({
        projectId,
        startDate: newSchedule.startDate,
        endDate: newSchedule.endDate,
        engineerCount: parseInt(newSchedule.engineerCount),
        totalHours: parseInt(newSchedule.totalHours),
        onsiteType: newSchedule.onsiteType,
        notes: newSchedule.notes || null
      });

      setNewSchedule({ startDate: '', endDate: '', engineerCount: 1, totalHours: 40, onsiteType: 'UAT', notes: '' });
      loadSchedules();
    } catch (error) {
      console.error('Error creating schedule:', error);
      alert('Failed to create onsite schedule');
    }
  };

  const handleUpdateSchedule = async (schedule) => {
    try {
      // Only send necessary fields, exclude navigation properties
      const updatePayload = {
        onsiteScheduleId: schedule.onsiteScheduleId,
        projectId: schedule.projectId,
        startDate: schedule.startDate,
        endDate: schedule.endDate,
        engineerCount: schedule.engineerCount,
        totalHours: schedule.totalHours,
        onsiteType: schedule.onsiteType,
        notes: schedule.notes || null
      };
      console.log('Updating schedule with payload:', updatePayload);
      console.log('Original schedule object:', schedule);
      await updateOnsiteSchedule(schedule.onsiteScheduleId, updatePayload);
      setEditingSchedule(null);
      loadSchedules();
    } catch (error) {
      console.error('Error updating schedule:', error);
      console.error('Error response:', error.response?.data);
      alert('Failed to update onsite schedule');
    }
  };

  const handleDeleteSchedule = async (id) => {
    if (!window.confirm('Remove this onsite week?')) {
      return;
    }

    try {
      await deleteOnsiteSchedule(id);
      loadSchedules();
    } catch (error) {
      console.error('Error deleting schedule:', error);
      alert('Failed to delete onsite schedule');
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  };

  return (
    <div className="onsite-schedule-manager">
      <h4>Onsite Schedules</h4>
      <p className="schedule-description">
        Schedule onsite periods (UAT, Go-Live, Hypercare, etc.). Hours are spread across ALL days (including weekends).
      </p>

      <div className="schedule-list">
        {schedules.length === 0 ? (
          <div className="no-schedules">No onsite schedules yet</div>
        ) : (
          <table className="schedule-table">
            <thead>
              <tr>
                <th>Start Date</th>
                <th>End Date</th>
                <th>Engineers</th>
                <th>Type</th>
                <th>Total Hours</th>
                <th>Notes</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {schedules.map(schedule => (
                <tr key={schedule.onsiteScheduleId} className={schedule.onsiteType.toLowerCase()}>
                  {editingSchedule?.onsiteScheduleId === schedule.onsiteScheduleId ? (
                    <>
                      <td>
                        <input
                          type="date"
                          value={editingSchedule.startDate.split('T')[0]}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            startDate: e.target.value
                          })}
                        />
                      </td>
                      <td>
                        <input
                          type="date"
                          value={editingSchedule.endDate.split('T')[0]}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            endDate: e.target.value
                          })}
                        />
                      </td>
                      <td>
                        <input
                          type="number"
                          min="1"
                          max="10"
                          value={editingSchedule.engineerCount}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            engineerCount: parseInt(e.target.value)
                          })}
                        />
                      </td>
                      <td>
                        <select
                          value={editingSchedule.onsiteType}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            onsiteType: e.target.value
                          })}
                        >
                          <option value="UAT">UAT</option>
                          <option value="GoLive">Go Live</option>
                          <option value="Training">Training</option>
                          <option value="PLCTesting">PLC Testing</option>
                          <option value="IntegrationTesting">Integration Testing</option>
                          <option value="Hypercare">Hypercare</option>
                          <option value="Custom">Custom</option>
                        </select>
                      </td>
                      <td>
                        <input
                          type="number"
                          min="1"
                          max="200"
                          value={editingSchedule.totalHours}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            totalHours: parseInt(e.target.value)
                          })}
                        />
                      </td>
                      <td>
                        <textarea
                          value={editingSchedule.notes || ''}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            notes: e.target.value
                          })}
                          placeholder="Trip details..."
                          rows="2"
                        />
                      </td>
                      <td>
                        <button type="button" className="btn-save" onClick={() => handleUpdateSchedule(editingSchedule)}>
                          Save
                        </button>
                        <button type="button" className="btn-cancel-small" onClick={() => setEditingSchedule(null)}>
                          Cancel
                        </button>
                      </td>
                    </>
                  ) : (
                    <>
                      <td className="week-date">{formatDate(schedule.startDate)}</td>
                      <td className="week-date">{formatDate(schedule.endDate)}</td>
                      <td>{schedule.engineerCount}</td>
                      <td>
                        <span className={`type-badge ${schedule.onsiteType.toLowerCase()}`}>
                          {schedule.onsiteType}
                        </span>
                      </td>
                      <td className="total-hours">{schedule.totalHours}h</td>
                      <td className="notes-cell">{schedule.notes || '-'}</td>
                      <td>
                        <button type="button" className="btn-edit-small" onClick={() => setEditingSchedule(schedule)}>
                          Edit
                        </button>
                        <button type="button" className="btn-delete-small" onClick={() => handleDeleteSchedule(schedule.onsiteScheduleId)}>
                          Delete
                        </button>
                      </td>
                    </>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="add-schedule">
        <h5>Add Onsite Schedule</h5>
        <div className="schedule-form">
          <div className="form-field">
            <label>Start Date</label>
            <input
              type="date"
              value={newSchedule.startDate}
              onChange={(e) => setNewSchedule({ ...newSchedule, startDate: e.target.value })}
            />
          </div>
          <div className="form-field">
            <label>End Date</label>
            <input
              type="date"
              value={newSchedule.endDate}
              onChange={(e) => setNewSchedule({ ...newSchedule, endDate: e.target.value })}
            />
          </div>
          <div className="form-field">
            <label>Engineers</label>
            <input
              type="number"
              min="1"
              max="10"
              value={newSchedule.engineerCount}
              onChange={(e) => setNewSchedule({ ...newSchedule, engineerCount: e.target.value })}
            />
          </div>
          <div className="form-field">
            <label>Type</label>
            <select
              value={newSchedule.onsiteType}
              onChange={(e) => setNewSchedule({ ...newSchedule, onsiteType: e.target.value })}
            >
              <option value="UAT">UAT</option>
              <option value="GoLive">Go Live</option>
              <option value="Training">Training</option>
              <option value="PLCTesting">PLC Testing</option>
              <option value="IntegrationTesting">Integration Testing</option>
              <option value="Hypercare">Hypercare</option>
              <option value="Custom">Custom</option>
            </select>
          </div>
          <div className="form-field">
            <label>Total Hours</label>
            <input
              type="number"
              min="1"
              max="200"
              value={newSchedule.totalHours}
              onChange={(e) => setNewSchedule({ ...newSchedule, totalHours: e.target.value })}
            />
          </div>
          <div className="form-field full-width">
            <label>Notes</label>
            <textarea
              value={newSchedule.notes}
              onChange={(e) => setNewSchedule({ ...newSchedule, notes: e.target.value })}
              placeholder="Trip details, requirements, etc."
              rows="3"
            />
          </div>
          <button type="button" className="btn-add" onClick={handleAddSchedule}>
            Add Schedule
          </button>
        </div>
      </div>
    </div>
  );
};

export default OnsiteScheduleManager;
