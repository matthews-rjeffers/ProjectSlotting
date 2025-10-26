import React, { useState, useEffect } from 'react';
import { createProject, updateProject, unassignProject } from '../api';
import OnsiteScheduleManager from './OnsiteScheduleManager';
import './ProjectForm.css';

function ProjectForm({ project, isAssigned, onSave, onCancel }) {
  const [formData, setFormData] = useState({
    projectNumber: '',
    customerName: '',
    customerCity: '',
    customerState: '',
    estimatedDevHours: '',
    estimatedOnsiteHours: '',
    goLiveDate: '',
    crpDate: '',
    uatDate: '',
    codeCompleteDate: '',
    jiraLink: '',
    startDate: '',
    bufferPercentage: 20
  });

  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);
  const [createdProject, setCreatedProject] = useState(null);
  const [hasUnsavedOnsiteData, setHasUnsavedOnsiteData] = useState(false);

  useEffect(() => {
    if (project) {
      setFormData({
        projectNumber: project.projectNumber || '',
        customerName: project.customerName || '',
        customerCity: project.customerCity || '',
        customerState: project.customerState || '',
        estimatedDevHours: project.estimatedDevHours || '',
        estimatedOnsiteHours: project.estimatedOnsiteHours || '',
        goLiveDate: project.goLiveDate ? project.goLiveDate.split('T')[0] : '',
        crpDate: project.crpdate ? project.crpdate.split('T')[0] : '',
        uatDate: project.uatdate ? project.uatdate.split('T')[0] : '',
        codeCompleteDate: project.codeCompleteDate ? project.codeCompleteDate.split('T')[0] : '',
        jiraLink: project.jiraLink || '',
        startDate: project.startDate ? project.startDate.split('T')[0] : '',
        bufferPercentage: project.bufferPercentage || 20
      });
    }
  }, [project]);

  const validate = () => {
    const newErrors = {};

    if (!formData.projectNumber.trim()) {
      newErrors.projectNumber = 'Project number is required';
    }

    if (!formData.customerName.trim()) {
      newErrors.customerName = 'Customer name is required';
    }

    if (!formData.estimatedDevHours || formData.estimatedDevHours <= 0) {
      newErrors.estimatedDevHours = 'Valid estimated dev hours required';
    }

    if (!formData.estimatedOnsiteHours || formData.estimatedOnsiteHours < 0) {
      newErrors.estimatedOnsiteHours = 'Valid estimated onsite hours required';
    }

    // CRP date is now optional for new projects

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    setSaving(true);

    try {
      const payload = {
        projectNumber: formData.projectNumber,
        customerName: formData.customerName,
        customerCity: formData.customerCity,
        customerState: formData.customerState,
        estimatedDevHours: parseFloat(formData.estimatedDevHours),
        estimatedOnsiteHours: parseFloat(formData.estimatedOnsiteHours),
        goLiveDate: formData.goLiveDate || null,
        crpdate: formData.crpDate || null,
        bufferPercentage: parseFloat(formData.bufferPercentage || 20),
        uatdate: formData.uatDate || null,
        codeCompleteDate: formData.codeCompleteDate || null,
        jiraLink: formData.jiraLink,
        startDate: formData.startDate || null
      };

      if (project && project.projectId) {
        await updateProject(project.projectId, { ...payload, projectId: project.projectId });
        onSave();
      } else {
        const response = await createProject(payload);
        // Store the created project so we can show onsite schedule manager
        setCreatedProject(response.data);
        alert('Project created! You can now add onsite schedules before closing.');
      }
    } catch (error) {
      console.error('Error saving project:', error);
      const errorMessage = error.response?.data?.message || error.response?.data || 'Failed to save project. Please try again.';
      alert(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: null }));
    }
  };

  const handleUnassign = async () => {
    if (!window.confirm(`Are you sure you want to unassign this project from its squad? This will remove all allocations but preserve onsite schedules.`)) {
      return;
    }

    try {
      await unassignProject(project.projectId);
      alert('Project unassigned successfully');
      onSave(); // Refresh parent data and close modal
    } catch (error) {
      console.error('Error unassigning project:', error);
      alert('Failed to unassign project. Please try again.');
    }
  };

  const handleCancel = () => {
    if (hasUnsavedOnsiteData) {
      if (!window.confirm('You have unsaved onsite schedule data (dates filled in but not added). Are you sure you want to cancel?')) {
        return;
      }
    }
    onCancel();
  };

  const handleUpdate = async (e) => {
    e.preventDefault();

    if (hasUnsavedOnsiteData) {
      if (!window.confirm('You have unsaved onsite schedule data (dates filled in but not added). Are you sure you want to update without adding this schedule?')) {
        return;
      }
    }

    await handleSubmit(e);
  };

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>{project ? 'Edit Project' : 'New Project'}</h2>
        <form onSubmit={handleSubmit}>
          {/* Three-column grid for top fields */}
          <div className="form-grid-three">
            <div className="form-group">
              <label>Project Number *</label>
              <input
                type="text"
                name="projectNumber"
                value={formData.projectNumber}
                onChange={handleChange}
                className={errors.projectNumber ? 'error' : ''}
                disabled={project && project.projectId}
              />
              {errors.projectNumber && <span className="error-message">{errors.projectNumber}</span>}
            </div>

            <div className="form-group">
              <label>Customer Name *</label>
              <input
                type="text"
                name="customerName"
                value={formData.customerName}
                onChange={handleChange}
                className={errors.customerName ? 'error' : ''}
              />
              {errors.customerName && <span className="error-message">{errors.customerName}</span>}
            </div>

            <div className="form-group">
              <label>Customer City</label>
              <input
                type="text"
                name="customerCity"
                value={formData.customerCity}
                onChange={handleChange}
              />
            </div>

            <div className="form-group">
              <label>Customer State</label>
              <input
                type="text"
                name="customerState"
                value={formData.customerState}
                onChange={handleChange}
                maxLength="2"
                placeholder="2-letter code"
              />
            </div>

            <div className="form-group">
              <label>Estimated Dev Hours *</label>
              <input
                type="number"
                name="estimatedDevHours"
                value={formData.estimatedDevHours}
                onChange={handleChange}
                step="0.5"
                min="0"
                className={errors.estimatedDevHours ? 'error' : ''}
              />
              {errors.estimatedDevHours && <span className="error-message">{errors.estimatedDevHours}</span>}
            </div>

            <div className="form-group">
              <label>Estimated Onsite Hours *</label>
              <input
                type="number"
                name="estimatedOnsiteHours"
                value={formData.estimatedOnsiteHours}
                onChange={handleChange}
                step="0.5"
                min="0"
                className={errors.estimatedOnsiteHours ? 'error' : ''}
              />
              {errors.estimatedOnsiteHours && <span className="error-message">{errors.estimatedOnsiteHours}</span>}
            </div>

            <div className="form-group">
              <label>Start Date</label>
              <input
                type="date"
                name="startDate"
                value={formData.startDate}
                onChange={handleChange}
              />
            </div>

            <div className="form-group">
              <label>Code Complete Date</label>
              <input
                type="date"
                name="codeCompleteDate"
                value={formData.codeCompleteDate}
                onChange={handleChange}
              />
              <small className="field-hint">When 90% of dev work completes</small>
            </div>

            <div className="form-group">
              <label>CRP Date</label>
              <input
                type="date"
                name="crpDate"
                value={formData.crpDate}
                onChange={handleChange}
                className={errors.crpDate ? 'error' : ''}
              />
              {errors.crpDate && <span className="error-message">{errors.crpDate}</span>}
              <small className="field-hint">Can be set using Schedule Suggestion</small>
            </div>

            <div className="form-group">
              <label>UAT Date</label>
              <input
                type="date"
                name="uatDate"
                value={formData.uatDate}
                onChange={handleChange}
              />
            </div>

            <div className="form-group">
              <label>Go-Live Date</label>
              <input
                type="date"
                name="goLiveDate"
                value={formData.goLiveDate}
                onChange={handleChange}
              />
            </div>

            <div className="form-group">
              <label>Buffer Percentage</label>
              <div className="buffer-input-wrapper">
                <input
                  type="number"
                  name="bufferPercentage"
                  value={formData.bufferPercentage}
                  onChange={handleChange}
                  min="0"
                  max="100"
                  step="5"
                />
                <span className="input-suffix">%</span>
              </div>
              <small className="field-hint">Extra time for schedule suggestions</small>
            </div>

            <div className="form-group form-group-span-2">
              <label>Jira Link</label>
              <input
                type="url"
                name="jiraLink"
                value={formData.jiraLink}
                onChange={handleChange}
                placeholder="https://jira.example.com/browse/PROJECT-123"
              />
            </div>
          </div>

          {(project?.projectId || createdProject?.projectId) && (
            <OnsiteScheduleManager
              projectId={project?.projectId || createdProject?.projectId}
              uatDate={formData.uatDate}
              goLiveDate={formData.goLiveDate}
              onUnsavedDataChange={setHasUnsavedOnsiteData}
            />
          )}

          <div className="form-actions">
            {isAssigned && project?.projectId && (
              <button
                type="button"
                onClick={handleUnassign}
                className="btn btn-unassign"
                disabled={saving}
              >
                Unassign from Squad
              </button>
            )}
            <button
              type="button"
              onClick={createdProject ? onSave : handleCancel}
              className="btn btn-secondary"
              disabled={saving}
            >
              {createdProject ? 'Done' : 'Cancel'}
            </button>
            {!createdProject && (
              <button
                type="button"
                onClick={handleUpdate}
                className="btn btn-primary"
                disabled={saving}
              >
                {saving ? 'Saving...' : (project ? 'Update' : 'Create Project')}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}

export default ProjectForm;
