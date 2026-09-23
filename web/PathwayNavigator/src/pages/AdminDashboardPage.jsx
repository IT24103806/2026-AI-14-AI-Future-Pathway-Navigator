import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function AdminDashboardPage() {
  const { user } = useAuth();
  return (
    <div className="dashboard-container">
      <div className="dashboard-header-card">
        <div className="dashboard-welcome">
          <span className="welcome-avatar">🛡️</span>
          <div><h1>Administration & Governance</h1><p className="dashboard-subtitle">Signed in as {user?.email}. Monitor controlled AI workflows without impersonating a student.</p></div>
        </div>
      </div>
      <div className="dashboard-grid">
        <div className="dash-card feature-card"><div className="feature-icon">✅</div><h3>Approval Oversight</h3><p>Inspect pending Reality Check workflows, their validation evidence, tool calls and human decisions.</p><Link className="btn btn-sm btn-primary mt-2" to="/counsellor/dashboard">Open Approval Centre</Link></div>
        <div className="dash-card feature-card"><div className="feature-icon">👥</div><h3>User & Role Governance</h3><p>Administrator responsibility: manage user access and role assignment. Student onboarding actions are intentionally unavailable here.</p></div>
        <div className="dash-card feature-card"><div className="feature-icon">📚</div><h3>Career Catalogue Governance</h3><p>Administrator responsibility: maintain approved career, education and market-data sources used by agents.</p></div>
        <div className="dash-card feature-card"><div className="feature-icon">🔎</div><h3>Audit & Safe Failure</h3><p>Review workflow IDs, deterministic validations, failures and approval records for accountability.</p></div>
      </div>
    </div>
  );
}
