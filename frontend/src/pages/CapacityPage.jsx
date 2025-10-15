import React, { useState, useEffect } from 'react';
import { getSquads, getProjects } from '../api';
import ImprovedCapacityView from '../components/ImprovedCapacityView';
import ProjectForm from '../components/ProjectForm';
import ProjectList from '../components/ProjectList';
import ExportData from '../components/ExportData';
import '../App.css';

function CapacityPage() {
  const [squads, setSquads] = useState([]);
  const [projects, setProjects] = useState([]);
  const [selectedSquad, setSelectedSquad] = useState(null);
  const [showProjectForm, setShowProjectForm] = useState(false);
  const [editingProject, setEditingProject] = useState(null);
  const [dateRange, setDateRange] = useState({
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date(new Date().setFullYear(new Date().getFullYear() + 2)).toISOString().split('T')[0]
  });

  useEffect(() => {
    loadSquads();
    loadProjects();
  }, []);

  const loadSquads = async () => {
    try {
      const response = await getSquads();
      setSquads(response.data);
      if (response.data.length > 0 && !selectedSquad) {
        setSelectedSquad(response.data[0].squadId);
      }
    } catch (error) {
      console.error('Error loading squads:', error);
    }
  };

  const loadProjects = async () => {
    try {
      const response = await getProjects();
      setProjects(response.data);
    } catch (error) {
      console.error('Error loading projects:', error);
    }
  };

  const handleProjectSaved = () => {
    loadProjects();
    setShowProjectForm(false);
    setEditingProject(null);
  };

  const handleEditProject = (project) => {
    setEditingProject(project);
    setShowProjectForm(true);
  };

  const handleDeleteProject = () => {
    loadProjects();
  };

  return (
    <div className="page-container capacity-page">
      <div className="page-header">
        <h1>Capacity Planning</h1>
        <div className="header-actions">
          <button onClick={() => setShowProjectForm(true)} className="btn btn-primary">
            + New Project
          </button>
          <ExportData projects={projects} squads={squads} />
        </div>
      </div>

      <div className="capacity-layout">
        <aside className="sidebar">
          <div className="sidebar-section">
            <h3>Squads</h3>
            {squads.length === 0 ? (
              <div className="empty-state">
                <p>No squads yet.</p>
              </div>
            ) : (
              <div className="squad-list">
                {squads.map(squad => (
                  <button
                    key={squad.squadId}
                    className={`squad-item ${selectedSquad === squad.squadId ? 'active' : ''}`}
                    onClick={() => setSelectedSquad(squad.squadId)}
                  >
                    <div className="squad-name">{squad.squadName}</div>
                    <div className="squad-lead">{squad.squadLeadName}</div>
                    <div className="squad-capacity">
                      {squad.teamMembers?.filter(tm => tm.isActive).length || 0} members
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="sidebar-section">
            <h3>Date Range</h3>
            <div className="date-range">
              <label>
                Start:
                <input
                  type="date"
                  value={dateRange.startDate}
                  onChange={(e) => setDateRange({ ...dateRange, startDate: e.target.value })}
                />
              </label>
              <label>
                End:
                <input
                  type="date"
                  value={dateRange.endDate}
                  onChange={(e) => setDateRange({ ...dateRange, endDate: e.target.value })}
                />
              </label>
            </div>
          </div>

          <div className="sidebar-section">
            <ProjectList
              projects={projects}
              onEdit={handleEditProject}
              onDelete={handleDeleteProject}
            />
          </div>
        </aside>

        <main className="main-content">
          {selectedSquad && (
            <ImprovedCapacityView
              squadId={selectedSquad}
              squad={squads.find(s => s.squadId === selectedSquad)}
              projects={projects}
              dateRange={dateRange}
              onProjectUpdate={loadProjects}
              onEditProject={handleEditProject}
            />
          )}
        </main>
      </div>

      {showProjectForm && (
        <ProjectForm
          project={editingProject}
          onSave={handleProjectSaved}
          onCancel={() => {
            setShowProjectForm(false);
            setEditingProject(null);
          }}
        />
      )}
    </div>
  );
}

export default CapacityPage;
