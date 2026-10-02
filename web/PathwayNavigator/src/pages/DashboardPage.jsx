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
  const progressPercent = totalStages > 0 ? Math.round((completedStages / totalStages) * 100) : 0;
  const topMatch = latestAnalysis?.recommendations?.[0];

  const profileSignals = [
    Boolean(profile?.academicStage),
    (profile?.coreSkills?.length || 0) > 0,
    (profile?.hobbiesInterests?.length || 0) > 0,
    Boolean(profile?.careerAmbitions),
  ];
  const profileCompleteness = Math.round(
    (profileSignals.filter(Boolean).length / profileSignals.length) * 100
  );

  const displayName = profile?.fullName || user?.email?.split('@')[0] || 'student';
  const initial = displayName.trim().charAt(0).toUpperCase();

  return (
    <div className="dashboard-container">
      {/* ------------------------------------------------------------- Hero */}
      <section className="dashboard-header-card">
        <div className="dashboard-welcome">
          <span className="welcome-avatar" aria-hidden="true">{initial || '👤'}</span>
          <div>
            <h1>
              Welcome back, {displayName}
              <span>
                {profile?.academicStage ? `${profile.academicStage} · ` : ''}
                Your PathwayNavigator workspace is ready.
              </span>
            </h1>
          </div>
        </div>

        <div className="dashboard-actions">
          <Link
            to="/onboarding?mode=update"
            className="btn btn-glass"
            title="Review and update your saved profile with Agent 1"
          >
            🤖 Re-run AI Onboarding
          </Link>
          <button onClick={logout} className="btn btn-outline-danger">
            Sign out
          </button>
        </div>
      </section>

      {/* ------------------------------------------------------------ Stats */}
      <section className="dash-stats" aria-label="Your progress at a glance">
        <div className="stat-tile stat-tile--mint">
          <span className="stat-tile__icon" aria-hidden="true">🎯</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">{topMatch ? `${topMatch.match_score}%` : '—'}</div>
            <div className="stat-tile__label">Top pathway match</div>
            <div className="stat-tile__hint">{topMatch ? topMatch.pathway_name : 'Run Career Discovery'}</div>
          </div>
        </div>

        <div className="stat-tile">
          <span className="stat-tile__icon" aria-hidden="true">👤</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">{profileCompleteness}%</div>
            <div className="stat-tile__label">Profile completeness</div>
            <div className="stat-tile__hint">
              {profileSignals.filter(Boolean).length}/4 onboarding signals captured
            </div>
          </div>
        </div>

        <div className="stat-tile stat-tile--violet">
          <span className="stat-tile__icon" aria-hidden="true">🗺️</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">
              {totalStages > 0 ? `${completedStages}/${totalStages}` : '—'}
            </div>
            <div className="stat-tile__label">Roadmap milestones</div>
            <div className="stat-tile__hint">
              {totalStages > 0 ? `${progressPercent}% completed` : 'Build a roadmap to start tracking'}
            </div>
          </div>
        </div>

        <div className="stat-tile stat-tile--amber">
          <span className="stat-tile__icon" aria-hidden="true">🛡️</span>
          <div className="stat-tile__body">
            <div className="stat-tile__value">{latestPlan?.status === 'ready' ? 'Ready' : 'Pending'}</div>
            <div className="stat-tile__label">Reality Check</div>
            <div className="stat-tile__hint">
              {latestPlan?.status === 'ready' ? 'Counsellor review loop available' : 'Submit a pathway to Agent 4'}
            </div>
          </div>
        </div>
      </section>

      {/* ------------------------------------------------- Onboarding nudge */}
      {!profile && (
        <section className="glass-panel dash-nudge">
          <span className="icon-chip" aria-hidden="true">🚀</span>
          <div className="dash-nudge__copy">
            <h3>Finish your profile to unlock personalised pathways</h3>
            <p>
              Agent 1 needs your academic stage, core skills, interests and ambition. The
              conversational setup takes about three minutes — or use the standard form.
            </p>
          </div>
          <Link to="/onboarding" className="btn btn-primary">Start AI onboarding</Link>
        </section>
      )}

      {/* ----------------------------------------------------- Profile card */}
      {profile && (
        <section className="dash-card student-profile-hero-card">
          <div className="profile-hero-header">
            <div>
              <span className="badge-agent">🤖 Verified by Agent 1 ({profile.onboardingMethod})</span>
              <h2>Target career: {profile.careerAmbitions}</h2>
            </div>
            <span className="stage-badge-large">{profile.academicStage}</span>
          </div>

          <div className="profile-details-grid">
            <div className="profile-section-box">
              <h4>💡 Core skills</h4>
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
              <h4>🎨 Hobbies &amp; interests</h4>
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
        </section>
      )}

      {/* ---------------------------------------------------- Quick actions */}
      <section className="quick-actions" aria-label="Recommended next steps">
        <Link to="/career-discovery" className="action-tile">
          <span className="action-tile__icon" aria-hidden="true">🧭</span>
          <h3>{latestAnalysis ? 'Continue your pathways' : 'Explore pathways'}</h3>
          <p>
            {topMatch
              ? `Top match: ${topMatch.pathway_name} (${topMatch.match_score}% match). Compare all ranked paths and market evidence.`
              : `Score your skills and ambitions against a curated career knowledge base with live market signals.`}
          </p>
        </Link>

        <Link to="/student/reality-check" className="action-tile">
          <span className="action-tile__icon" aria-hidden="true">🛡️</span>
          <h3>Reality Check &amp; approval</h3>
          <p>
            Review Agent 4 feasibility evidence, skill gaps, the counsellor decision and your
            shortest gap-closing plan.
          </p>
        </Link>

        <Link to="/onboarding?mode=update" className="action-tile">
          <span className="action-tile__icon" aria-hidden="true">🤖</span>
          <h3>{profile ? 'Update your profile' : 'AI onboarding'}</h3>
          <p>
            {profile
              ? 'Re-run Agent 1 to refresh your name, academic stage, skills, interests or ambition — every change is saved to your profile.'
              : 'Set up your academic stage, skills, interests and ambition with the AI guide.'}
          </p>
        </Link>

        <div className="action-tile action-tile--static">
          <span className="action-tile__icon" aria-hidden="true">📚</span>
          <h3>Learning modules</h3>
          <p>
            Interactive modules, quizzes and project milestones aligned with{' '}
            {profile?.careerAmbitions || 'your ambitions'}.
          </p>
          <span className="data-source-badge data-source-simulated">Coming soon</span>
        </div>
      </section>

      {/* ------------------------------------------------ Roadmap + session */}
      <section className="dashboard-grid">
        <div className="dash-card">
          <div className="progress-panel">
            <div
              className="stat-ring"
              style={{ '--value': progressPercent }}
              aria-label={`Roadmap progress ${progressPercent} percent`}
            >
              {progressPercent}%
            </div>
            <div className="progress-panel__meta">
              <h3>Roadmap progress</h3>
              {latestPlan?.status === 'ready' ? (
                <>
                  <p>
                    <strong>{latestPlan.selected_pathway}</strong> · {completedStages}/{totalStages} milestones
                    completed.
                  </p>
                  <p className="text-dim">Next action: {latestPlan.next_action}</p>
                  <Link to="/career-discovery" className="btn btn-sm btn-primary mt-2">
                    Open roadmap tracker
                  </Link>
                </>
              ) : (
                <>
                  <p className="text-dim">
                    No active roadmap yet. Choose a pathway and Agent 3 will build a step-by-step plan.
                  </p>
                  <Link to="/career-discovery" className="btn btn-sm btn-primary mt-2">
                    Build my roadmap
                  </Link>
                </>
              )}
            </div>
          </div>
        </div>

        <div className="dash-card">
          <h3>Session &amp; identity</h3>
          <div className="info-list">
            <div className="info-item">
              <span className="info-label">User ID</span>
              <span className="info-value code-font">{user?.userId}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Email</span>
              <span className="info-value">{user?.email}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Assigned role</span>
              <span className={`role-badge role-${user?.role?.toLowerCase()}`}>{user?.role}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Token expiration</span>
              <span className="info-value">{formatDate(user?.expiresAt)}</span>
            </div>
          </div>

          <details className="token-details">
            <summary>🔐 View JWT token snippet</summary>
            <pre className="token-preview">{token ? `${token.substring(0, 45)}...` : 'No token'}</pre>
          </details>
        </div>
      </section>
    </div>
  );
};

export default DashboardPage;
