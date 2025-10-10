import React, { useState, useEffect } from 'react';
import { getProjectOnsiteSchedules, createOnsiteSchedule, updateOnsiteSchedule, deleteOnsiteSchedule } from '../api';
import './OnsiteScheduleManager.css';

const OnsiteScheduleManager = ({ projectId, uatDate, goLiveDate }) => {
  const [schedules, setSchedules] = useState([]);
  const [editingSchedule, setEditingSchedule] = useState(null);
  const [newSchedule, setNewSchedule] = useState({
    weekStartDate: '',
    engineerCount: 1,
    onsiteType: 'UAT'
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
    if (!newSchedule.weekStartDate) {
      alert('Please select a week start date');
      return;
    }

    try {
      await createOnsiteSchedule({
        projectId,
        weekStartDate: newSchedule.weekStartDate,
        engineerCount: parseInt(newSchedule.engineerCount),
        onsiteType: newSchedule.onsiteType
      });

      setNewSchedule({ weekStartDate: '', engineerCount: 1, onsiteType: 'UAT' });
      loadSchedules();
    } catch (error) {
      console.error('Error creating schedule:', error);
      alert('Failed to create onsite schedule');
    }
  };

  const handleUpdateSchedule = async (schedule) => {
    try {
      await updateOnsiteSchedule(schedule.onsiteScheduleId, schedule);
      setEditingSchedule(null);
      loadSchedules();
    } catch (error) {
      console.error('Error updating schedule:', error);
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

  const getWeekMonday = (dateStr) => {
    // Parse date in local timezone to avoid timezone shift issues
    const [year, month, day] = dateStr.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    const dayOfWeek = date.getDay();
    const diff = date.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
    const monday = new Date(date.getFullYear(), date.getMonth(), diff);

    // Format as YYYY-MM-DD
    const yyyy = monday.getFullYear();
    const mm = String(monday.getMonth() + 1).padStart(2, '0');
    const dd = String(monday.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  };

  return (
    <div className="onsite-schedule-manager">
      <h4>Onsite Schedule</h4>
      <p className="schedule-description">
        Schedule specific weeks for onsite work. Each entry allocates 40 hours per engineer per week.
      </p>

      <div className="schedule-list">
        {schedules.length === 0 ? (
          <div className="no-schedules">No onsite weeks scheduled</div>
        ) : (
          <table className="schedule-table">
            <thead>
              <tr>
                <th>Week Starting</th>
                <th>Engineers</th>
                <th>Type</th>
                <th>Total Hours</th>
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
                          value={editingSchedule.weekStartDate.split('T')[0]}
                          onChange={(e) => setEditingSchedule({
                            ...editingSchedule,
                            weekStartDate: e.target.value
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
                        </select>
                      </td>
                      <td>{editingSchedule.engineerCount * 40}h</td>
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
                      <td className="week-date">{formatDate(schedule.weekStartDate)}</td>
                      <td>{schedule.engineerCount}</td>
                      <td>
                        <span className={`type-badge ${schedule.onsiteType.toLowerCase()}`}>
                          {schedule.onsiteType}
                        </span>
                      </td>
                      <td className="total-hours">{schedule.engineerCount * 40}h</td>
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
        <h5>Add Onsite Week</h5>
        <div className="schedule-form">
          <div className="form-field">
            <label>Week Starting (Monday)</label>
            <input
              type="date"
              value={newSchedule.weekStartDate}
              onChange={(e) => {
                const monday = getWeekMonday(e.target.value);
                setNewSchedule({ ...newSchedule, weekStartDate: monday });
              }}
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
            </select>
          </div>
          <div className="form-field">
            <label>Total Hours</label>
            <div className="hours-display">{newSchedule.engineerCount * 40}h</div>
          </div>
          <button type="button" className="btn-add" onClick={handleAddSchedule}>
            Add Week
          </button>
        </div>
      </div>
    </div>
  );
};

export default OnsiteScheduleManager;
