import React, { useState, useEffect } from 'react';
import { getSquads, deleteSquad } from '../api';
import SquadManagement from '../components/SquadManagement';
import '../App.css';

function SquadsPage() {
  const [squads, setSquads] = useState([]);
  const [showSquadManagement, setShowSquadManagement] = useState(false);
  const [editingSquad, setEditingSquad] = useState(null);

  useEffect(() => {
    loadSquads();
  }, []);

  const loadSquads = async () => {
    try {
      const response = await getSquads();
      setSquads(response.data);
    } catch (error) {
      console.error('Error loading squads:', error);
    }
  };

  const handleEditSquad = (squad) => {
    setEditingSquad(squad);
    setShowSquadManagement(true);
  };

  const handleDeleteSquad = async (squad) => {
    // Check if squad has team members
    const activeMembers = squad.teamMembers?.filter(tm => tm.isActive).length || 0;
    if (activeMembers > 0) {
      alert(`Cannot delete ${squad.squadName}. This squad has ${activeMembers} active team member(s). Please remove all team members before deleting the squad.`);
      return;
    }

    if (!window.confirm(`Are you sure you want to delete ${squad.squadName}? This action cannot be undone.`)) {
      return;
    }

    try {
      await deleteSquad(squad.squadId);
      loadSquads(); // Refresh the squad list
    } catch (error) {
      console.error('Error deleting squad:', error);
      if (error.response?.data?.message) {
        alert(error.response.data.message);
      } else {
        alert('Failed to delete squad. Please try again.');
      }
    }
  };

  const handleCloseManagement = () => {
    setShowSquadManagement(false);
    setEditingSquad(null);
  };

  return (
    <div className="page-container">
      <div className="page-header">
        <h1>Manage Squads</h1>
        <button onClick={() => setShowSquadManagement(true)} className="btn btn-primary">
          + New Squad
        </button>
      </div>

      <div className="squads-grid">
        {squads.length === 0 ? (
          <div className="empty-state">
            <p>No squads yet. Create your first squad to get started.</p>
            <button onClick={() => setShowSquadManagement(true)} className="btn btn-primary">
              Create First Squad
            </button>
          </div>
        ) : (
          squads.map(squad => (
            <div key={squad.squadId} className="squad-card">
              <div className="squad-card-header">
                <h3>{squad.squadName}</h3>
                <div style={{ display: 'flex', gap: '8px' }}>
                  <button
                    onClick={() => handleEditSquad(squad)}
                    className="btn btn-small btn-secondary"
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => handleDeleteSquad(squad)}
                    className="btn btn-small btn-danger"
                  >
                    Delete
                  </button>
                </div>
              </div>
              <div className="squad-card-body">
                <div className="squad-detail">
                  <label>Squad Lead:</label>
                  <span>{squad.squadLeadName}</span>
                </div>
                <div className="squad-detail">
                  <label>Team Members:</label>
                  <span>{squad.teamMembers?.filter(tm => tm.isActive).length || 0} active</span>
                </div>
                <div className="squad-detail">
                  <label>Daily Capacity:</label>
                  <span>
                    {squad.teamMembers
                      ?.filter(tm => tm.isActive)
                      .reduce((sum, tm) => sum + (tm.dailyCapacityHours || 0), 0)
                      .toFixed(1)} hours
                  </span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {showSquadManagement && (
        <SquadManagement
          initialSquad={editingSquad}
          onClose={handleCloseManagement}
          onSquadsUpdated={loadSquads}
        />
      )}
    </div>
  );
}

export default SquadsPage;
