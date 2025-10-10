import React, { useState, useEffect } from 'react';
import { getProjects, getSquads, getProjectAllocations, previewProjectAllocation, allocateProject } from '../api';
import AllocationPreviewModal from '../components/AllocationPreviewModal';
import ProjectForm from '../components/ProjectForm';
import './ProjectsPage.css';

const ProjectsPage = () => {
  const [projects, setProjects] = useState([]);
  const [squads, setSquads] = useState([]);
  const [projectAllocations, setProjectAllocations] = useState({});
  const [loading, setLoading] = useState(true);
  const [previewModal, setPreviewModal] = useState({ isOpen: false, preview: null, projectId: null, squadId: null, projectName: '' });
  const [editingProject, setEditingProject] = useState(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [projectsRes, squadsRes] = await Promise.all([
        getProjects(),
        getSquads()
      ]);

      const projectsData = projectsRes.data;
      setProjects(projectsData);
      setSquads(squadsRes.data);

      // Get allocations for all projects
      const allocationsMap = {};
      await Promise.all(
        projectsData.map(async (project) => {
          try {
            const allocRes = await getProjectAllocations(project.projectId);
            if (allocRes.data && allocRes.data.length > 0) {
              allocationsMap[project.projectId] = allocRes.data[0].squadId;
            }
          } catch (err) {
            // Project not allocated
          }
        })
      );

      setProjectAllocations(allocationsMap);
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  };

  const getSquadName = (squadId) => {
    const squad = squads.find(s => s.squadId === squadId);
    return squad ? squad.squadName : 'Unassigned';
  };

  const getProjectDisplayName = (project) => {
    return `${project.projectNumber} - ${project.customerName}`;
  };

  const handlePreviewAllocation = async (projectId, squadId) => {
    const project = projects.find(p => p.projectId === projectId);
    try {
      const response = await previewProjectAllocation(projectId, squadId);
      setPreviewModal({
        isOpen: true,
        preview: response.data,
        projectId,
        squadId,
        projectName: getProjectDisplayName(project)
      });
    } catch (error) {
      console.error('Error previewing allocation:', error);
      alert('Failed to preview allocation');
    }
  };

  const handleConfirmAllocation = async () => {
    try {
      await allocateProject(previewModal.projectId, previewModal.squadId);
      setPreviewModal({ isOpen: false, preview: null, projectId: null, squadId: null, projectName: '' });
      loadData();
    } catch (error) {
      console.error('Error allocating project:', error);
      alert('Failed to allocate project');
    }
  };

  const handleEditProject = (project) => {
    setEditingProject(project);
  };

  const handleSaveProject = () => {
    setEditingProject(null);
    loadData();
  };

  const handleCancelEdit = () => {
    setEditingProject(null);
  };

  const sortedProjects = [...projects].sort((a, b) => {
    const dateA = a.crpdate ? new Date(a.crpdate) : new Date(9999, 11, 31);
    const dateB = b.crpdate ? new Date(b.crpdate) : new Date(9999, 11, 31);
    return dateA - dateB;
  });

  if (loading) {
    return <div className="loading">Loading projects...</div>;
  }

  return (
    <div className="projects-page">
      <div className="page-header">
        <div>
          <h1>Projects</h1>
          <p className="subtitle">View all projects and their squad assignments</p>
        </div>
      </div>

      <div className="projects-table-container">
        <table className="projects-table">
          <thead>
            <tr>
              <th>Project Name</th>
              <th>Start Date</th>
              <th>CRP Date</th>
              <th>Go Live Date</th>
              <th>Dev Hours</th>
              <th>Onsite Hours</th>
              <th>Assigned Squad</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sortedProjects.map(project => (
              <tr key={project.projectId}>
                <td className="project-name">{getProjectDisplayName(project)}</td>
                <td>{formatDate(project.startDate)}</td>
                <td className="crp-date">{formatDate(project.crpdate)}</td>
                <td>{formatDate(project.goLiveDate)}</td>
                <td className="hours dev-hours">{project.estimatedDevHours}h</td>
                <td className="hours onsite-hours">{project.estimatedOnsiteHours}h</td>
                <td className="squad-cell">
                  {projectAllocations[project.projectId] ? (
                    <span className="squad-badge assigned">
                      {getSquadName(projectAllocations[project.projectId])}
                    </span>
                  ) : (
                    <span className="squad-badge unassigned">Unassigned</span>
                  )}
                </td>
                <td className="actions-cell">
                  <div className="action-buttons">
                    <button
                      className="btn-edit"
                      onClick={() => handleEditProject(project)}
                      title="Edit Project"
                    >
                      Edit
                    </button>
                    {!projectAllocations[project.projectId] && (
                      <select
                        className="squad-select"
                        onChange={(e) => {
                          if (e.target.value) {
                            handlePreviewAllocation(project.projectId, parseInt(e.target.value));
                            e.target.value = '';
                          }
                        }}
                        defaultValue=""
                      >
                        <option value="">Assign to squad...</option>
                        {squads.map(squad => (
                          <option key={squad.squadId} value={squad.squadId}>
                            {squad.squadName}
                          </option>
                        ))}
                      </select>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <AllocationPreviewModal
        isOpen={previewModal.isOpen}
        onClose={() => setPreviewModal({ isOpen: false, preview: null, projectId: null, squadId: null, projectName: '' })}
        preview={previewModal.preview}
        projectName={previewModal.projectName}
        onConfirm={handleConfirmAllocation}
      />

      {editingProject && (
        <ProjectForm
          project={editingProject}
          onSave={handleSaveProject}
          onCancel={handleCancelEdit}
        />
      )}
    </div>
  );
};

export default ProjectsPage;
