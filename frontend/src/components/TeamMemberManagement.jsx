import React, { useState, useEffect } from 'react';
import { getTeamMembers, createTeamMember, updateTeamMember, deleteTeamMember } from '../api';
import './TeamMemberManagement.css';

function TeamMemberManagement({ squad, onBack }) {
  const [teamMembers, setTeamMembers] = useState([]);
  const [editingMember, setEditingMember] = useState(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState({
    memberName: '',
    role: 'Developer',
    dailyCapacityHours: 6.5
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadTeamMembers();
  }, [squad.squadId]);

  const loadTeamMembers = async () => {
    try {
      const response = await getTeamMembers(squad.squadId);
      setTeamMembers(response.data);
    } catch (error) {
      console.error('Error loading team members:', error);
    } finally {
      setLoading(false);
    }
  };

  const validate = () => {
    const newErrors = {};

    if (!formData.memberName.trim()) {
      newErrors.memberName = 'Name is required';
    }

    if (!formData.role) {
      newErrors.role = 'Role is required';
    }

    if (!formData.dailyCapacityHours || formData.dailyCapacityHours <= 0 || formData.dailyCapacityHours > 24) {
      newErrors.dailyCapacityHours = 'Capacity must be between 0 and 24 hours';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    try {
      const payload = {
        squadId: squad.squadId,
        memberName: formData.memberName,
        role: formData.role,
        dailyCapacityHours: parseFloat(formData.dailyCapacityHours),
        isActive: true
      };

      if (editingMember) {
        await updateTeamMember(editingMember.teamMemberId, { ...payload, teamMemberId: editingMember.teamMemberId });
      } else {
        await createTeamMember(payload);
      }

      await loadTeamMembers();
      resetForm();
    } catch (error) {
      console.error('Error saving team member:', error);
      alert('Failed to save team member. Please try again.');
    }
  };

  const handleEdit = (member) => {
    setEditingMember(member);
    setFormData({
      memberName: member.memberName,
      role: member.role,
      dailyCapacityHours: member.dailyCapacityHours
    });
    setShowAddForm(true);
  };

  const handleDelete = async (member) => {
    if (!window.confirm(`Are you sure you want to remove ${member.memberName} from the team?`)) {
      return;
    }

    try {
      await deleteTeamMember(member.teamMemberId);
      await loadTeamMembers();
    } catch (error) {
      console.error('Error deleting team member:', error);
      alert('Failed to remove team member. Please try again.');
    }
  };

  const resetForm = () => {
    setFormData({ memberName: '', role: 'Developer', dailyCapacityHours: 6.5 });
    setEditingMember(null);
    setShowAddForm(false);
    setErrors({});
  };

  const groupedMembers = {
    'Squad Lead': teamMembers.filter(m => m.role === 'SquadLead' && m.isActive),
    'Project Lead': teamMembers.filter(m => m.role === 'ProjectLead' && m.isActive),
    'Developer': teamMembers.filter(m => m.role === 'Developer' && m.isActive)
  };

  const totalCapacity = teamMembers
    .filter(m => m.isActive)
    .reduce((sum, m) => sum + m.dailyCapacityHours, 0);

  return (
    <div className="team-member-management">
      <div className="management-header">
        <button onClick={onBack} className="btn btn-secondary">
          ← Back to Squads
        </button>
        <h2>{squad.squadName} - Team Management</h2>
        <div className="team-stats">
          <div className="stat">
            <strong>Total Members:</strong> {teamMembers.filter(m => m.isActive).length}
          </div>
          <div className="stat">
            <strong>Daily Capacity:</strong> {totalCapacity.toFixed(1)} hours
          </div>
        </div>
      </div>

      {loading ? (
        <div className="loading">Loading team members...</div>
      ) : (
        <>
          <div className="management-actions">
            {!showAddForm && (
              <button
                onClick={() => setShowAddForm(true)}
                className="btn btn-primary"
              >
                + Add Team Member
              </button>
            )}
          </div>

          {showAddForm && (
            <form onSubmit={handleSubmit} className="member-form">
              <h3>{editingMember ? 'Edit Team Member' : 'Add New Team Member'}</h3>

              <div className="form-row">
                <div className="form-group">
                  <label>Name *</label>
                  <input
                    type="text"
                    value={formData.memberName}
                    onChange={(e) => setFormData({ ...formData, memberName: e.target.value })}
                    className={errors.memberName ? 'error' : ''}
                    placeholder="e.g., John Doe"
                  />
                  {errors.memberName && <span className="error-message">{errors.memberName}</span>}
                </div>

                <div className="form-group">
                  <label>Role *</label>
                  <select
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    className={errors.role ? 'error' : ''}
                  >
                    <option value="SquadLead">Squad Lead</option>
                    <option value="ProjectLead">Project Lead</option>
                    <option value="Developer">Developer</option>
                  </select>
                  {errors.role && <span className="error-message">{errors.role}</span>}
                </div>

                <div className="form-group">
                  <label>Daily Capacity (hours) *</label>
                  <input
                    type="number"
                    step="0.5"
                    min="0"
                    max="24"
                    value={formData.dailyCapacityHours}
                    onChange={(e) => setFormData({ ...formData, dailyCapacityHours: e.target.value })}
                    className={errors.dailyCapacityHours ? 'error' : ''}
                    placeholder="6.5"
                  />
                  {errors.dailyCapacityHours && <span className="error-message">{errors.dailyCapacityHours}</span>}
                  <small className="form-hint">Typical range: 4-8 hours per day</small>
                </div>
              </div>

              <div className="form-actions">
                <button type="button" onClick={resetForm} className="btn btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary">
                  {editingMember ? 'Update' : 'Add'}
                </button>
              </div>
            </form>
          )}

          <div className="team-members-list">
            {Object.entries(groupedMembers).map(([role, members]) => (
              <div key={role} className="role-group">
                <h3>{role}s ({members.length})</h3>
                {members.length === 0 ? (
                  <div className="empty-state">No {role.toLowerCase()}s yet.</div>
                ) : (
                  <div className="members-grid">
                    {members.map(member => (
                      <div key={member.teamMemberId} className="member-card">
                        <div className="member-header">
                          <div className="member-name">{member.memberName}</div>
                          <div className="member-actions">
                            <button
                              onClick={() => handleEdit(member)}
                              className="btn-icon"
                              title="Edit"
                            >
                              ✏️
                            </button>
                            <button
                              onClick={() => handleDelete(member)}
                              className="btn-icon"
                              title="Remove"
                            >
                              🗑️
                            </button>
                          </div>
                        </div>
                        <div className="member-info">
                          <div className="info-item">
                            <span className="label">Role:</span>
                            <span className="value">{member.role}</span>
                          </div>
                          <div className="info-item">
                            <span className="label">Daily Capacity:</span>
                            <span className="value capacity-badge">{member.dailyCapacityHours} hrs</span>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

export default TeamMemberManagement;
