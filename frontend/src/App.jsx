import React from 'react';
import { BrowserRouter as Router, Routes, Route, NavLink } from 'react-router-dom';
import SquadsPage from './pages/SquadsPage';
import TeamsPage from './pages/TeamsPage';
import CapacityPage from './pages/CapacityPage';
import ProjectsPage from './pages/ProjectsPage';
import './App.css';

function App() {
  return (
    <Router>
      <div className="app">
        <header className="app-header">
          <h1>Project Scheduler</h1>
          <nav className="main-nav">
            <NavLink to="/capacity" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
              Capacity Planning
            </NavLink>
            <NavLink to="/projects" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
              Projects
            </NavLink>
            <NavLink to="/squads" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
              Manage Squads
            </NavLink>
            <NavLink to="/teams" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
              Manage Teams
            </NavLink>
          </nav>
        </header>

        <Routes>
          <Route path="/" element={<CapacityPage />} />
          <Route path="/capacity" element={<CapacityPage />} />
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/squads" element={<SquadsPage />} />
          <Route path="/teams" element={<TeamsPage />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
