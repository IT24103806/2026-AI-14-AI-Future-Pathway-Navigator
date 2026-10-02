import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { formatDate } from '../utils/tokenUtils';
import { getStudentProfileApi } from '../api/profileApi';
import { getLatestPathwayAnalysisApi, getLatestPathwayPlanApi } from '../api/pathwayApi';

const DashboardPage = () => {
  const { user, token, logout } = useAuth();
  const [profile, setProfile] = useState(null);
  const [latestAnalysis, setLatestAnalysis] = useState(null);
  const [latestPlan, setLatestPlan] = useState(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const [profileData, analysisData, planData] = await Promise.all([
          getStudentProfileApi().catch(() => null),
          getLatestPathwayAnalysisApi().catch(() => null),
          getLatestPathwayPlanApi().catch(() => null),
        ]);
        if (profileData) setProfile(profileData);
        if (analysisData) setLatestAnalysis(analysisData);
        if (planData) setLatestPlan(planData);
      } catch (err) {
        console.warn('Dashboard data fetch warning:', err);
      }
    };

    fetchDashboardData();
  }, []);

  const completedStages = (latestPlan?.roadmap || []).filter((s) => s.status === 'completed').length;
  const totalStages = (latestPlan?.roadmap || []).length;

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
            {latestAnalysis?.recommendations?.length > 0
              ? `Top match: ${latestAnalysis.recommendations[0].pathway_name} (${latestAnalysis.recommendations[0].match_score}% match).`
              : `Explore customized AI career paths and skill trees tailored for your ${profile?.academicStage || 'academic'} profile.`}
          </p>
          {latestPlan?.status === 'ready' && (
            <p className="text-dim" style={{ marginTop: '0.5rem', fontSize: '0.85rem' }}>
              🗺️ Active Roadmap: <strong>{latestPlan.selected_pathway}</strong> ({completedStages}/{totalStages} milestones completed)
            </p>
          )}
          <Link to="/career-discovery" className="btn btn-sm btn-primary mt-2">
            {latestAnalysis ? 'Continue Pathways & Roadmap' : 'Explore Pathways'}
          </Link>
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
          <div className="feature-icon">🛡️</div><h3>Reality Check & Approval</h3>
          <p>View Agent 4 feasibility evidence, skill gaps, counsellor decision and your shortest gap-closing plan.</p>
          <Link to="/student/reality-check" className="btn btn-sm btn-primary mt-2">View Reality Check</Link>
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
