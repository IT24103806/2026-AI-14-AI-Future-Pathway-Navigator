import React from 'react';
import { useAuth } from '../hooks/useAuth';
import { formatDate } from '../utils/tokenUtils';

const DashboardPage = () => {
  const { user, token, logout } = useAuth();

  return (
    <div className="dashboard-container">
      <div className="dashboard-header-card">
        <div className="dashboard-welcome">
          <span className="welcome-avatar">👤</span>
          <div>
            <h1>Welcome, {user?.email}</h1>
            <p className="dashboard-subtitle">
              Your PathwayNavigator session is active.
            </p>
          </div>
        </div>
        <div className="dashboard-actions">
          <button onClick={logout} className="btn btn-outline-danger">
            Sign Out
          </button>
        </div>
      </div>

      <div className="dashboard-grid">
        <div className="dash-card profile-summary-card">
          <h3>Session & Identity Info</h3>
          <div className="info-list">
            <div className="info-item">
              <span className="info-label">User ID:</span>
              <span className="info-value code-font">{user?.userId}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Email:</span>
              <span className="info-value">{user?.email}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Assigned Role:</span>
              <span className={`role-badge role-${user?.role?.toLowerCase()}`}>
                {user?.role}
              </span>
            </div>
            <div className="info-item">
              <span className="info-label">Token Expiration:</span>
              <span className="info-value">{formatDate(user?.expiresAt)}</span>
            </div>
          </div>
        </div>

        <div className="dash-card feature-card">
          <div className="feature-icon">🤖</div>
          <h3>AI Pathway Recommender</h3>
          <p>
            Explore customized AI career paths and skill trees tailored for your profile.
          </p>
          <button className="btn btn-sm btn-primary mt-2">Explore Pathways</button>
        </div>

        <div className="dash-card feature-card">
          <div className="feature-icon">📚</div>
          <h3>Learning Modules</h3>
          <p>
            Access interactive modules, quizzes, and project milestones assigned to your role.
          </p>
          <button className="btn btn-sm btn-secondary mt-2">View Modules</button>
        </div>

        <div className="dash-card feature-card">
          <div className="feature-icon">🛡️</div>
          <h3>Security & JWT Access</h3>
          <p>
            Your JWT Bearer token is verified by backend policy and automatically attached to requests.
          </p>
          <details className="token-details">
            <summary>View Token Snippet</summary>
            <pre className="token-preview">{token ? `${token.substring(0, 45)}...` : 'No Token'}</pre>
          </details>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
