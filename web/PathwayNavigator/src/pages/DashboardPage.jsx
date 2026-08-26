import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { formatDate } from '../utils/tokenUtils';
import { getStudentProfileApi } from '../api/profileApi';

const DashboardPage = () => {
  const { user, token, logout } = useAuth();
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const data = await getStudentProfileApi();
        setProfile(data);
      } catch (err) {
        console.warn('Profile not yet created or error fetching:', err);
      }
    };

    fetchProfile();
  }, []);

  return (
    <div className="dashboard-container">
      <div className="dashboard-header-card">
        <div className="dashboard-welcome">
          <span className="welcome-avatar">👤</span>
          <div>
            <h1>Welcome, {profile?.fullName || user?.email}</h1>
            <p className="dashboard-subtitle">
              {profile?.academicStage ? `${profile.academicStage} • ` : ''}Your PathwayNavigator session is active.
            </p>
          </div>
        </div>
        <div className="dashboard-actions">
          <Link to="/onboarding" className="btn btn-outline-primary" style={{ marginRight: '0.75rem' }}>
            🤖 Re-run AI Onboarding
          </Link>
          <button onClick={logout} className="btn btn-outline-danger">
            Sign Out
          </button>
        </div>
      </div>

      {/* Student Profile Overview Card */}
      {profile && (
        <div className="dash-card student-profile-hero-card" style={{ marginBottom: '2rem' }}>
          <div className="profile-hero-header">
            <div>
              <span className="badge-agent">🤖 Verified by Agent 1 ({profile.onboardingMethod})</span>
              <h2 style={{ marginTop: '0.5rem' }}>Target Career: {profile.careerAmbitions}</h2>
            </div>
            <span className="stage-badge-large">{profile.academicStage}</span>
          </div>

          <div className="profile-details-grid">
            <div className="profile-section-box">
              <h4>💡 Core Skills</h4>
              <div className="slot-tags-container">
                {profile.coreSkills?.length > 0 ? (
                  profile.coreSkills.map((skill, idx) => (
                    <span key={idx} className="slot-pill">{skill}</span>
                  ))
                ) : (
                  <span className="text-dim">No skills added</span>
                )}
              </div>
            </div>

            <div className="profile-section-box">
              <h4>🎨 Hobbies & Interests</h4>
              <div className="slot-tags-container">
                {profile.hobbiesInterests?.length > 0 ? (
                  profile.hobbiesInterests.map((hobby, idx) => (
                    <span key={idx} className="slot-pill hobby-pill">{hobby}</span>
                  ))
                ) : (
                  <span className="text-dim">No interests added</span>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

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
            Explore customized AI career paths and skill trees tailored for your {profile?.academicStage || 'academic'} profile.
          </p>
          <Link to="/career-discovery" className="btn btn-sm btn-primary mt-2">Explore Pathways</Link>
        </div>

        <div className="dash-card feature-card">
          <div className="feature-icon">📚</div>
          <h3>Learning Modules</h3>
          <p>
            Access interactive modules, quizzes, and project milestones aligned with {profile?.careerAmbitions || 'your ambitions'}.
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

