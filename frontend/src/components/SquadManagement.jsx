import React, { useState, useEffect } from 'react';
import { getSquads, createSquad, updateSquad, deleteSquad, getTeamMembers } from '../api';
import TeamMemberManagement from './TeamMemberManagement';
import './SquadManagement.css';

function SquadManagement({ onClose, onSquadsUpdated }) {
  const [squads, setSquads] = useState([]);
  const [editingSquad, setEditingSquad] = useState(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [selectedSquad, setSelectedSquad] = useState(null);
  const [formData, setFormData] = useState({
    squadName: '',
    squadLeadName: ''
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadSquads();
  }, []);

  const loadSquads = async () => {
    try {
      const response = await getSquads();
      setSquads(response.data);
    } catch (error) {
      console.error('Error loading squads:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.squadName.trim()) {
      setErrors({ squadName: 'Squad name is required' });
      return;
    }

    try {
      const payload = {
        squadName: formData.squadName,
        squadLeadName: formData.squadLeadName || 'TBD',
        isActive: true
      };

      if (editingSquad) {
        await updateSquad(editingSquad.squadId, { ...payload, squadId: editingSquad.squadId });
      } else {
        await createSquad(payload);
      }

      await loadSquads();
      onSquadsUpdated();
      resetForm();
    } catch (error) {
      console.error('Error saving squad:', error);
      alert('Failed to save squad. Please try again.');
    }
  };

  const handleEdit = (squad) => {
    setEditingSquad(squad);
    setFormData({
      squadName: squad.squadName,
      squadLeadName: squad.squadLeadName
    });
    setShowAddForm(true);
  };

  const handleDelete = async (squad) => {
    if (!window.confirm(`Are you sure you want to delete ${squad.squadName}? This cannot be undone if the squad has allocations.`)) {
      return;
    }

    try {
      await deleteSquad(squad.squadId);
      await loadSquads();
      onSquadsUpdated();
    } catch (error) {
      console.error('Error deleting squad:', error);
      if (error.response?.data?.message) {
        alert(error.response.data.message);
      } else {
        alert('Failed to delete squad. Please try again.');
      }
    }
  };

  const resetForm = () => {
    setFormData({ squadName: '', squadLeadName: '' });
    setEditingSquad(null);
    setShowAddForm(false);
    setErrors({});
  };

  const handleManageTeam = (squad) => {
    setSelectedSquad(squad);
  };

  if (selectedSquad) {
    return (
      <TeamMemberManagement
        squad={selectedSquad}
        onBack={() => {
          setSelectedSquad(null);
          loadSquads();
          onSquadsUpdated();
        }}
      />
    );
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-large" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Squad Management</h2>
          <button onClick={onClose} className="btn-close">×</button>
        </div>

        {loading ? (
          <div className="loading">Loading squads...</div>
        ) : (
          <>
            <div className="squad-management-actions">
              {!showAddForm && (
                <button
                  onClick={() => setShowAddForm(true)}
                  className="btn btn-primary"
                >
                  + Add Squad
                </button>
              )}
            </div>

            {showAddForm && (
              <form onSubmit={handleSubmit} className="squad-form">
                <h3>{editingSquad ? 'Edit Squad' : 'Add New Squad'}</h3>
                <div className="form-row">
                  <div className="form-group">
                    <label>Squad Name *</label>
                    <input
                      type="text"
                      value={formData.squadName}
                      onChange={(e) => setFormData({ ...formData, squadName: e.target.value })}
                      className={errors.squadName ? 'error' : ''}
                      placeholder="e.g., Squad Delta"
                    />
                    {errors.squadName && <span className="error-message">{errors.squadName}</span>}
                  </div>

                  <div className="form-group">
                    <label>Squad Lead Name</label>
                    <input
                      type="text"
                      value={formData.squadLeadName}
                      onChange={(e) => setFormData({ ...formData, squadLeadName: e.target.value })}
                      placeholder="e.g., John Doe"
                    />
                  </div>
                </div>

                <div className="form-actions">
                  <button type="button" onClick={resetForm} className="btn btn-secondary">
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    {editingSquad ? 'Update' : 'Create'}
                  </button>
                </div>
              </form>
            )}

            <div className="squads-list">
              <h3>Current Squads</h3>
              {squads.length === 0 ? (
                <div className="empty-state">No squads yet. Click "Add Squad" to create one.</div>
              ) : (
                <div className="squads-grid">
                  {squads.map(squad => (
                    <div key={squad.squadId} className="squad-card">
                      <div className="squad-card-header">
                        <h4>{squad.squadName}</h4>
                        <div className="squad-card-actions">
                          <button
                            onClick={() => handleEdit(squad)}
                            className="btn-icon"
                            title="Edit Squad"
                          >
                            ✏️
                          </button>
                          <button
                            onClick={() => handleDelete(squad)}
                            className="btn-icon"
                            title="Delete Squad"
                          >
                            🗑️
                          </button>
                        </div>
                      </div>

                      <div className="squad-card-body">
                        <div className="squad-info">
                          <strong>Squad Lead:</strong> {squad.squadLeadName}
                        </div>
                        <div className="squad-info">
                          <strong>Team Size:</strong> {squad.teamMembers?.filter(tm => tm.isActive).length || 0} members
                        </div>
                        <div className="squad-info">
                          <strong>Daily Capacity:</strong>{' '}
                          {squad.teamMembers?.filter(tm => tm.isActive).reduce((sum, tm) => sum + tm.dailyCapacityHours, 0).toFixed(1) || 0} hours
                        </div>
                      </div>

                      <div className="squad-card-footer">
                        <button
                          onClick={() => handleManageTeam(squad)}
                          className="btn btn-small btn-primary"
                        >
                          Manage Team
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}

export default SquadManagement;
