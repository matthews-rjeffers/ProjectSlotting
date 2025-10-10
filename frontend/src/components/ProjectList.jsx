import React, { useState } from 'react';
import { deleteProject } from '../api';
import './ProjectList.css';

function ProjectList({ projects, onEdit, onDelete }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [deleting, setDeleting] = useState(null);

  const handleDelete = async (projectId) => {
    if (!window.confirm('Are you sure you want to delete this project? This will remove all allocations.')) {
      return;
    }

    setDeleting(projectId);
    try {
      await deleteProject(projectId);
      onDelete();
    } catch (error) {
      console.error('Error deleting project:', error);
      alert('Failed to delete project. Please try again.');
    } finally {
      setDeleting(null);
    }
  };

  const filteredProjects = projects.filter(project =>
    project.projectNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
    project.customerName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="project-list-container">
      <h3>Projects ({projects.length})</h3>

      <div className="search-box">
        <input
          type="text"
          placeholder="Search projects..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </div>

      <div className="project-list">
        {filteredProjects.length === 0 ? (
          <div className="empty-state">
            {searchTerm ? 'No projects found' : 'No projects yet'}
          </div>
        ) : (
          filteredProjects.map(project => (
            <div key={project.projectId} className="project-item">
              <div className="project-header">
                <div className="project-number">{project.projectNumber}</div>
                <div className="project-actions">
                  <button
                    onClick={() => onEdit(project)}
                    className="btn-icon"
                    title="Edit"
                  >
                    ✏️
                  </button>
                  <button
                    onClick={() => handleDelete(project.projectId)}
                    className="btn-icon"
                    title="Delete"
                    disabled={deleting === project.projectId}
                  >
                    {deleting === project.projectId ? '...' : '🗑️'}
                  </button>
                </div>
              </div>
              <div className="project-customer">{project.customerName}</div>
              {project.customerCity && project.customerState && (
                <div className="project-location">
                  {project.customerCity}, {project.customerState}
                </div>
              )}
              <div className="project-hours">
                <span>Dev: {project.estimatedDevHours}h</span>
                <span>Onsite: {project.estimatedOnsiteHours}h</span>
              </div>
              <div className="project-dates">
                <div>CRP: {new Date(project.crpdate).toLocaleDateString()}</div>
                {project.goLiveDate && (
                  <div>Go-Live: {new Date(project.goLiveDate).toLocaleDateString()}</div>
                )}
              </div>
              {project.jiraLink && (
                <a href={project.jiraLink} target="_blank" rel="noopener noreferrer" className="jira-link">
                  View in Jira →
                </a>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}

export default ProjectList;
