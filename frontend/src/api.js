import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Squads API
export const getSquads = () => api.get('/squads');
export const getSquad = (id) => api.get(`/squads/${id}`);
export const getSquadCapacity = (id, startDate, endDate) =>
  api.get(`/squads/${id}/capacity`, { params: { startDate, endDate } });
export const createSquad = (data) => api.post('/squads', data);
export const updateSquad = (id, data) => api.put(`/squads/${id}`, data);
export const deleteSquad = (id) => api.delete(`/squads/${id}`);

// Projects API
export const getProjects = () => api.get('/projects');
export const getProject = (id) => api.get(`/projects/${id}`);
export const createProject = (data) => api.post('/projects', data);
export const updateProject = (id, data) => api.put(`/projects/${id}`, data);
export const deleteProject = (id) => api.delete(`/projects/${id}`);
export const allocateProject = (id, squadId, startDate) =>
  api.post(`/projects/${id}/allocate`, { squadId, startDate });
export const reallocateProject = (id, squadId, startDate) =>
  api.post(`/projects/${id}/reallocate`, { squadId, startDate });
export const unassignProject = (id) =>
  api.post(`/projects/${id}/unassign`);
export const getProjectAllocations = (id) => api.get(`/projects/${id}/allocations`);
export const canAllocateProject = (squadId, startDate, crpDate, totalDevHours) =>
  api.post('/projects/can-allocate', { squadId, startDate, crpDate, totalDevHours });
export const previewProjectAllocation = (projectId, squadId) =>
  api.get(`/projects/${projectId}/preview-allocation/${squadId}`);

// Team Members API
export const getTeamMembers = (squadId) =>
  api.get('/teammembers', { params: squadId ? { squadId } : {} });
export const createTeamMember = (data) => api.post('/teammembers', data);
export const updateTeamMember = (id, data) => api.put(`/teammembers/${id}`, data);
export const deleteTeamMember = (id) => api.delete(`/teammembers/${id}`);

// Onsite Schedules API
export const getProjectOnsiteSchedules = (projectId) =>
  api.get(`/onsiteschedules/project/${projectId}`);
export const createOnsiteSchedule = (data) => api.post('/onsiteschedules', data);
export const updateOnsiteSchedule = (id, data) => api.put(`/onsiteschedules/${id}`, data);
export const deleteOnsiteSchedule = (id) => api.delete(`/onsiteschedules/${id}`);

export default api;
