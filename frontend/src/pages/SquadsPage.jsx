import React, { useState, useEffect } from 'react';
import { getSquads } from '../api';
import SquadManagement from '../components/SquadManagement';
import '../App.css';

function SquadsPage() {
  const [squads, setSquads] = useState([]);
  const [showSquadManagement, setShowSquadManagement] = useState(false);

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
                <button
                  onClick={() => setShowSquadManagement(true)}
                  className="btn btn-small btn-secondary"
                >
                  Edit
                </button>
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
          onClose={() => setShowSquadManagement(false)}
          onSquadsUpdated={loadSquads}
        />
      )}
    </div>
  );
}

export default SquadsPage;
