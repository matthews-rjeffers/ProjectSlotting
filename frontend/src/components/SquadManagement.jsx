import React, { useState, useEffect } from 'react';
import { getSquads, createSquad, updateSquad, deleteSquad, getTeamMembers } from '../api';
import TeamMemberManagement from './TeamMemberManagement';
import './SquadManagement.css';

function SquadManagement({ onClose, onSquadsUpdated, initialSquad }) {
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
    if (initialSquad) {
      // If initialSquad is provided, go directly to edit mode for that squad
      setEditingSquad(initialSquad);
      setFormData({
        squadName: initialSquad.squadName,
        squadLeadName: initialSquad.squadLeadName
      });
      setShowAddForm(true); // Show the form immediately
      setLoading(false);
    } else {
      // When used from SquadsPage with no initialSquad, assume we want to add a new squad
      // Don't load squads list - just show the add form
      setShowAddForm(true);
      setLoading(false);
    }
  }, [initialSquad]);

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

      onSquadsUpdated();
      onClose(); // Close the modal after successful save
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

  const handleDelete = async () => {
    if (!editingSquad) return;

    // Check if squad has team members
    const activeMembers = editingSquad.teamMembers?.filter(tm => tm.isActive).length || 0;
    if (activeMembers > 0) {
      alert(`Cannot delete ${editingSquad.squadName}. This squad has ${activeMembers} active team member(s). Please remove all team members before deleting the squad.`);
      return;
    }

    if (!window.confirm(`Are you sure you want to delete ${editingSquad.squadName}? This action cannot be undone.`)) {
      return;
    }

    try {
      await deleteSquad(editingSquad.squadId);
      onSquadsUpdated();
      onClose();
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
          <div className="loading">Loading...</div>
        ) : (
          <form onSubmit={handleSubmit} className="squad-form">
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
              <button type="button" onClick={onClose} className="btn btn-secondary">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                {editingSquad ? 'Update Squad' : 'Create Squad'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

export default SquadManagement;
