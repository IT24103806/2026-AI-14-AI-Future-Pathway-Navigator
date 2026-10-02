import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

const GOVERNANCE_CARDS = [
  {
    icon: '✅',
    title: 'Approval Oversight',
    text: 'Inspect pending Reality Check workflows, their validation evidence, tool calls and human decisions.',
    to: '/counsellor/dashboard',
    action: 'Open Approval Centre',
  },
  {
    icon: '👥',
    title: 'User & Role Governance',
    text: 'Manage user access and role assignment. Student onboarding actions are intentionally unavailable here.',
  },
  {
    icon: '📚',
    title: 'Career Catalogue Governance',
    text: 'Maintain the approved career, education and market-data sources that the agents rely on.',
  },
  {
    icon: '🔎',
    title: 'Audit & Safe Failure',
    text: 'Review workflow IDs, deterministic validations, failures and approval records for accountability.',
  },
];

export default function AdminDashboardPage() {
  const { user } = useAuth();

  return (
    <div className="dashboard-container">
      <section className="dashboard-header-card">
        <div className="dashboard-welcome">
          <span className="welcome-avatar" aria-hidden="true">🛡️</span>
          <div>
            <h1>
              Administration &amp; Governance
              <span>Signed in as {user?.email}. Monitor controlled AI workflows without impersonating a student.</span>
            </h1>
          </div>
        </div>
        <div className="dashboard-actions">
          <Link to="/counsellor/dashboard" className="btn btn-primary">
            ✅ Approval Centre
          </Link>
        </div>
      </section>

      <section className="dash-stats" aria-label="Governance responsibilities">
        <div className="stat-tile">
          <span className="stat-tile__icon" aria-hidden="true">🧾</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">Audit</div>
            <div className="stat-tile__label">Workflow traceability</div>
            <div className="stat-tile__hint">Every agent run carries a workflow ID</div>
          </div>
        </div>
        <div className="stat-tile stat-tile--mint">
          <span className="stat-tile__icon" aria-hidden="true">🔐</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">Role-based</div>
            <div className="stat-tile__label">Access control</div>
            <div className="stat-tile__hint">Student, Counsellor and Admin scopes</div>
          </div>
        </div>
        <div className="stat-tile stat-tile--violet">
          <span className="stat-tile__icon" aria-hidden="true">🛡️</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">Human</div>
            <div className="stat-tile__label">Decision authority</div>
            <div className="stat-tile__hint">High-impact paths require review</div>
          </div>
        </div>
      </section>

      <section className="dashboard-grid">
        {GOVERNANCE_CARDS.map((card) => (
          <div className="dash-card feature-card" key={card.title}>
            <div className="feature-icon" aria-hidden="true">{card.icon}</div>
            <h3>{card.title}</h3>
            <p>{card.text}</p>
            {card.to && (
              <Link className="btn btn-sm btn-primary mt-2" to={card.to}>
                {card.action}
              </Link>
            )}
          </div>
        ))}
      </section>
    </div>
  );
}
