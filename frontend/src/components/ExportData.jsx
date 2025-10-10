import React from 'react';
import './ExportData.css';

function ExportData({ projects, squads }) {
  const exportToCSV = () => {
    const headers = [
      'Project Number',
      'Customer Name',
      'Customer City',
      'Customer State',
      'Estimated Dev Hours',
      'Estimated Onsite Hours',
      'Start Date',
      'CRP Date',
      'UAT Date',
      'Go-Live Date',
      'Jira Link',
      'Allocated Squad'
    ];

    const rows = projects.map(project => {
      // Find which squad(s) this project is allocated to
      const allocatedSquads = project.projectAllocations?.length > 0
        ? [...new Set(project.projectAllocations.map(a => {
            const squad = squads.find(s => s.squadId === a.squadId);
            return squad?.squadName || '';
          }))].join(', ')
        : 'Unallocated';

      return [
        project.projectNumber,
        project.customerName,
        project.customerCity || '',
        project.customerState || '',
        project.estimatedDevHours,
        project.estimatedOnsiteHours,
        project.startDate ? new Date(project.startDate).toLocaleDateString() : '',
        new Date(project.crpdate).toLocaleDateString(),
        project.uatdate ? new Date(project.uatdate).toLocaleDateString() : '',
        project.goLiveDate ? new Date(project.goLiveDate).toLocaleDateString() : '',
        project.jiraLink || '',
        allocatedSquads
      ];
    });

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');

    downloadFile(csvContent, 'projects-export.csv', 'text/csv');
  };

  const exportToJSON = () => {
    const data = {
      exportDate: new Date().toISOString(),
      projects: projects.map(project => ({
        ...project,
        allocatedSquads: project.projectAllocations?.length > 0
          ? [...new Set(project.projectAllocations.map(a => {
              const squad = squads.find(s => s.squadId === a.squadId);
              return squad?.squadName || '';
            }))]
          : []
      })),
      squads: squads.map(squad => ({
        squadId: squad.squadId,
        squadName: squad.squadName,
        squadLeadName: squad.squadLeadName,
        teamSize: squad.teamMembers?.filter(tm => tm.isActive).length || 0,
        dailyCapacity: squad.teamMembers?.filter(tm => tm.isActive).reduce((sum, tm) => sum + tm.dailyCapacityHours, 0) || 0
      }))
    };

    const jsonContent = JSON.stringify(data, null, 2);
    downloadFile(jsonContent, 'projects-export.json', 'application/json');
  };

  const downloadFile = (content, filename, mimeType) => {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="export-data">
      <button onClick={exportToCSV} className="btn btn-secondary btn-small">
        Export CSV
      </button>
      <button onClick={exportToJSON} className="btn btn-secondary btn-small">
        Export JSON
      </button>
    </div>
  );
}

export default ExportData;
