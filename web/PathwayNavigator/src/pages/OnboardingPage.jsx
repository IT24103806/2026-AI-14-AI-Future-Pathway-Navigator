import React, { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { getProfileStatusApi, getStudentProfileApi } from '../api/profileApi';
import AgentChatWindow from '../components/onboarding/AgentChatWindow';
import SlotProgressSidebar from '../components/onboarding/SlotProgressSidebar';
import StandardOnboardingForm from '../components/onboarding/StandardOnboardingForm';
import JourneySteps from '../components/common/JourneySteps';
import AlertBanner from '../components/common/AlertBanner';
import { EMPTY_PROFILE_SLOTS, slotsFromProfile } from '../utils/profileSlots';

/**
 * Onboarding screen. It serves two purposes:
 *
 *   - first-time onboarding: the student has no completed profile yet, so the AI guide / standard
 *     form collect the four slots and save them;
 *   - profile update ("Re-run AI onboarding"): reached with `?mode=update` from the dashboard, or
 *     automatically when a student who already finished onboarding opens this page from the navbar.
 *     The saved profile is loaded so the chat and the form start from those values, and every
 *     change is written back to the student's profile (StudentProfiles + Users.FullName).
 */
const OnboardingPage = () => {
  const [searchParams] = useSearchParams();
  const requestedUpdateMode = searchParams.get('mode') === 'update';

  const [mode, setMode] = useState('chat'); // 'chat' or 'form'
  const [isUpdateMode, setIsUpdateMode] = useState(requestedUpdateMode);
  const [currentSlots, setCurrentSlots] = useState(EMPTY_PROFILE_SLOTS);
  const [savedProfile, setSavedProfile] = useState(null);
  const [baselineSlots, setBaselineSlots] = useState(null);
  const [isCheckingStatus, setIsCheckingStatus] = useState(true);
  const [loadError, setLoadError] = useState('');

  const { user } = useAuth();

  useEffect(() => {
    let isActive = true;

    const loadPage = async () => {
      try {
        const status = await getProfileStatusApi();

        // Onboarding is already done: the page becomes a profile update instead of bouncing the
        // student back to the dashboard, so every "AI Onboarding" link stays useful.
        const updateMode = requestedUpdateMode || status.isOnboardingCompleted;
        if (updateMode) {
          setIsUpdateMode(true);

          // A profile may not exist yet (onboarding skipped earlier); the chat still works from scratch.
          const profile = await getStudentProfileApi().catch(() => null);
          if (!isActive || !profile) return;

          const slots = slotsFromProfile(profile);
          setSavedProfile(profile);
          setCurrentSlots(slots);
          setBaselineSlots(slots);
        }
      } catch (err) {
        console.error('Status check error on onboarding load:', err);
        if (isActive) setLoadError(err.message || 'Could not load your profile. You can still continue — changes are saved when you finish.');
      } finally {
        if (isActive) setIsCheckingStatus(false);
      }
    };

    loadPage();

    return () => {
      isActive = false;
    };
  }, [requestedUpdateMode]);

  const handleSlotsUpdate = (newSlots) => {
    setCurrentSlots((prev) => ({
      ...prev,
      ...newSlots,
    }));
  };

  if (isCheckingStatus) {
    return (
      <div className="full-page-loader">
        <div className="spinner" />
        <p>{isUpdateMode ? 'Loading your saved profile...' : 'Checking onboarding status...'}</p>
      </div>
    );
  }

  const displayName =
    savedProfile?.fullName || user?.fullName || user?.email?.split('@')[0] || 'there';

  return (
    <div className="onboarding-page-container">
      {/* Onboarding Header Banner */}
      <div className="onboarding-hero">
        <div className="orb-field" aria-hidden="true">
          <span className="orb orb--cyan" style={{ width: 220, height: 220, top: -70, left: '6%' }} />
          <span className="orb orb--violet" style={{ width: 240, height: 240, bottom: -100, right: '4%' }} />
        </div>
        <div className="onboarding-hero-content">
          <span className="hero-pill-badge">
            {isUpdateMode ? '♻️ Update your profile' : '✨ Step 1: Student Onboarding'}
          </span>
          <h1>
            {isUpdateMode
              ? `Let's keep your profile up to date, ${displayName}!`
              : `Welcome to PathwayNavigator, ${displayName}!`}
          </h1>
          <p>
            {isUpdateMode
              ? 'Agent 1 already has your details. Tell it what has changed — a new skill, a new academic stage, an updated ambition — or edit everything in the standard form. Your saved profile and pathway recommendations update straight away.'
              : "Let's personalize your career pathways and learning roadmap. Choose how you'd like to set up your profile:"}
          </p>
          {!isUpdateMode && <JourneySteps current={1} className="mt-4" />}

          {isUpdateMode && (
            <div className="onboarding-update-actions">
              <Link to="/dashboard" className="btn btn-glass btn-sm">
                ← Back to dashboard
              </Link>
              {savedProfile?.updatedAt && (
                <span className="text-dim">
                  Last updated {new Date(savedProfile.updatedAt).toLocaleDateString()}
                </span>
              )}
            </div>
          )}

          {/* Mode Switcher Tabs */}
          <div className="onboarding-mode-switcher">
            <button
              type="button"
              className={`mode-tab-btn ${mode === 'chat' ? 'active-mode' : ''}`}
              onClick={() => setMode('chat')}
            >
              🤖 Interactive AI Guide (Recommended)
            </button>
            <button
              type="button"
              className={`mode-tab-btn ${mode === 'form' ? 'active-mode' : ''}`}
              onClick={() => setMode('form')}
            >
              📝 Standard Form
            </button>
          </div>
        </div>
      </div>

      {/* Main Mode View */}
      <div className="onboarding-content-layout">
        <AlertBanner type="error" message={loadError} onClose={() => setLoadError('')} />
      </div>

      <div className="onboarding-content-layout">
        {mode === 'chat' ? (
          <div className="chat-layout-grid">
            <div className="chat-main-column">
              <AgentChatWindow
                mode={isUpdateMode ? 'update' : 'create'}
                onSlotsUpdate={handleSlotsUpdate}
                currentSlots={currentSlots}
                baselineSlots={baselineSlots}
                savedProfile={savedProfile}
              />
            </div>
            <div className="chat-sidebar-column">
              <SlotProgressSidebar slots={currentSlots} isUpdateMode={isUpdateMode} />
            </div>
          </div>
        ) : (
          <div className="form-layout-container">
            <StandardOnboardingForm
              mode={isUpdateMode ? 'update' : 'create'}
              initialProfile={savedProfile}
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default OnboardingPage;
