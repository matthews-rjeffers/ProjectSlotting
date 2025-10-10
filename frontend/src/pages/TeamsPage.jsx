import React, { useState, useEffect } from 'react';
import { getSquads, createTeamMember, updateTeamMember, deleteTeamMember } from '../api';
import '../App.css';

function TeamsPage() {
  const [squads, setSquads] = useState([]);
  const [selectedSquadId, setSelectedSquadId] = useState(null);
  const [showMemberForm, setShowMemberForm] = useState(false);
  const [editingMember, setEditingMember] = useState(null);
  const [formData, setFormData] = useState({
    memberName: '',
    role: 'Developer',
    dailyCapacityHours: 6.5,
    isActive: true
  });

  useEffect(() => {
    loadSquads();
  }, []);

  const loadSquads = async () => {
    try {
      const response = await getSquads();
      setSquads(response.data);
      if (response.data.length > 0 && !selectedSquadId) {
        setSelectedSquadId(response.data[0].squadId);
      }
    } catch (error) {
      console.error('Error loading squads:', error);
    }
  };

  const selectedSquad = squads.find(s => s.squadId === selectedSquadId);

  const handleAddMember = () => {
    setEditingMember(null);
    setFormData({
      memberName: '',
      role: 'Developer',
      dailyCapacityHours: 6.5,
      isActive: true
    });
    setShowMemberForm(true);
  };

  const handleEditMember = (member) => {
    setEditingMember(member);
    setFormData({
      memberName: member.memberName,
      role: member.role,
      dailyCapacityHours: member.dailyCapacityHours,
      isActive: member.isActive,
      squadId: member.squadId
    });
    setShowMemberForm(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingMember) {
        await updateTeamMember(editingMember.teamMemberId, {
          ...formData,
          squadId: formData.squadId || selectedSquadId
        });
      } else {
        await createTeamMember({
          ...formData,
          squadId: formData.squadId || selectedSquadId
        });
      }
      setShowMemberForm(false);
      setEditingMember(null);
      loadSquads();
    } catch (error) {
      console.error('Error saving team member:', error);
      alert('Failed to save team member');
    }
  };

  const handleDelete = async (memberId) => {
    if (!confirm('Are you sure you want to delete this team member?')) return;
    try {
      await deleteTeamMember(memberId);
      loadSquads();
    } catch (error) {
      console.error('Error deleting team member:', error);
      alert('Failed to delete team member');
    }
  };

  const handleToggleActive = async (member) => {
    try {
      await updateTeamMember(member.teamMemberId, {
        ...member,
        isActive: !member.isActive
      });
      loadSquads();
    } catch (error) {
      console.error('Error updating team member:', error);
      alert('Failed to update team member');
    }
  };

  return (
    <div className="page-container">
      <div className="page-header">
        <h1>Manage Team Members</h1>
        {selectedSquadId && (
          <button onClick={handleAddMember} className="btn btn-primary">
            + Add Team Member
          </button>
        )}
      </div>

      <div className="teams-layout">
        <div className="squad-selector">
          <h3>Select Squad</h3>
          {squads.map(squad => (
            <button
              key={squad.squadId}
              className={`squad-button ${selectedSquadId === squad.squadId ? 'active' : ''}`}
              onClick={() => setSelectedSquadId(squad.squadId)}
            >
              <div className="squad-name">{squad.squadName}</div>
              <div className="squad-count">
                {squad.teamMembers?.filter(tm => tm.isActive).length || 0} members
              </div>
            </button>
          ))}
        </div>

        <div className="team-members-list">
          {selectedSquad ? (
            <>
              <div className="squad-info">
                <h2>{selectedSquad.squadName}</h2>
                <p>Squad Lead: {selectedSquad.squadLeadName}</p>
                <p>
                  Total Capacity: {' '}
                  {selectedSquad.teamMembers
                    ?.filter(tm => tm.isActive)
                    .reduce((sum, tm) => sum + (tm.dailyCapacityHours || 0), 0)
                    .toFixed(1)} hours/day
                </p>
              </div>

              <table className="team-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Role</th>
                    <th>Daily Hours</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedSquad.teamMembers && selectedSquad.teamMembers.length > 0 ? (
                    selectedSquad.teamMembers
                      .sort((a, b) => {
                        // Define role order: Squad Lead, Project Lead, Developer
                        const roleOrder = { 'Squad Lead': 1, 'Project Lead': 2, 'Developer': 3 };
                        const roleCompare = (roleOrder[a.role] || 99) - (roleOrder[b.role] || 99);
                        if (roleCompare !== 0) return roleCompare;
                        // Within same role, sort by name
                        return a.memberName.localeCompare(b.memberName);
                      })
                      .map(member => (
                      <tr key={member.teamMemberId} className={!member.isActive ? 'inactive' : ''}>
                        <td>{member.memberName}</td>
                        <td>{member.role}</td>
                        <td>{member.dailyCapacityHours} hrs</td>
                        <td>
                          <span className={`status-badge ${member.isActive ? 'active' : 'inactive'}`}>
                            {member.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="actions">
                          <button
                            onClick={() => handleEditMember(member)}
                            className="btn btn-small btn-secondary"
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => handleToggleActive(member)}
                            className="btn btn-small btn-secondary"
                          >
                            {member.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                          <button
                            onClick={() => handleDelete(member.teamMemberId)}
                            className="btn btn-small btn-danger"
                          >
                            Delete
                          </button>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="5" className="empty-cell">
                        No team members yet. Add your first team member.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </>
          ) : (
            <div className="empty-state">
              <p>Select a squad to manage team members</p>
            </div>
          )}
        </div>
      </div>

      {showMemberForm && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <h2>{editingMember ? 'Edit Team Member' : 'Add Team Member'}</h2>
              <button
                className="close-button"
                onClick={() => setShowMemberForm(false)}
              >
                ×
              </button>
            </div>
            <form onSubmit={handleSubmit} className="modal-body">
              <div className="form-group">
                <label>Name *</label>
                <input
                  type="text"
                  value={formData.memberName}
                  onChange={(e) => setFormData({ ...formData, memberName: e.target.value })}
                  required
                />
              </div>

              <div className="form-group">
                <label>Squad *</label>
                <select
                  value={formData.squadId || selectedSquadId}
                  onChange={(e) => setFormData({ ...formData, squadId: parseInt(e.target.value) })}
                  required
                >
                  {squads.map(squad => (
                    <option key={squad.squadId} value={squad.squadId}>
                      {squad.squadName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label>Role *</label>
                <select
                  value={formData.role}
                  onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                  required
                >
                  <option value="Squad Lead">Squad Lead</option>
                  <option value="Project Lead">Project Lead</option>
                  <option value="Developer">Developer</option>
                </select>
              </div>

              <div className="form-group">
                <label>Daily Capacity Hours *</label>
                <input
                  type="number"
                  step="0.5"
                  min="0"
                  max="24"
                  value={formData.dailyCapacityHours}
                  onChange={(e) => setFormData({ ...formData, dailyCapacityHours: parseFloat(e.target.value) })}
                  required
                />
              </div>

              <div className="form-group checkbox-group">
                <label>
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  />
                  Active
                </label>
              </div>

              <div className="modal-actions">
                <button type="button" onClick={() => setShowMemberForm(false)} className="btn btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary">
                  {editingMember ? 'Update' : 'Add'} Member
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default TeamsPage;
