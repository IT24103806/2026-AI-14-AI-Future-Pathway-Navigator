import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { getProfileStatusApi } from '../api/profileApi';
import AgentChatWindow from '../components/onboarding/AgentChatWindow';
import SlotProgressSidebar from '../components/onboarding/SlotProgressSidebar';
import StandardOnboardingForm from '../components/onboarding/StandardOnboardingForm';

const OnboardingPage = () => {
  const [mode, setMode] = useState('chat'); // 'chat' or 'form'
  const [currentSlots, setCurrentSlots] = useState({
    academic_stage: null,
    core_skills: [],
    hobbies_interests: [],
    career_ambitions: null,
  });
  const [isCheckingStatus, setIsCheckingStatus] = useState(true);

  const { user } = useAuth();
  const navigate = useNavigate();

  // If user already completed onboarding, redirect to dashboard
  useEffect(() => {
    const checkStatus = async () => {
      try {
        const status = await getProfileStatusApi();
        if (status.isOnboardingCompleted) {
          navigate('/dashboard', { replace: true });
        }
      } catch (err) {
        console.error('Status check error on onboarding load:', err);
      } finally {
        setIsCheckingStatus(false);
      }
    };

    checkStatus();
  }, [navigate]);

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
        <p>Checking onboarding status...</p>
      </div>
    );
  }

  return (
    <div className="onboarding-page-container">
      {/* Onboarding Header Banner */}
      <div className="onboarding-hero">
        <div className="onboarding-hero-content">
          <span className="hero-pill-badge">✨ Step 1: Student Onboarding</span>
          <h1>Welcome to PathwayNavigator, {user?.fullName || user?.email?.split('@')[0]}!</h1>
          <p>
            Let's personalize your career pathways and learning roadmap. Choose how you'd like to set up your profile:
          </p>

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
        {mode === 'chat' ? (
          <div className="chat-layout-grid">
            <div className="chat-main-column">
              <AgentChatWindow
                onSlotsUpdate={handleSlotsUpdate}
                currentSlots={currentSlots}
              />
            </div>
            <div className="chat-sidebar-column">
              <SlotProgressSidebar slots={currentSlots} />
            </div>
          </div>
        ) : (
          <div className="form-layout-container">
            <StandardOnboardingForm />
          </div>
        )}
      </div>
    </div>
  );
};

export default OnboardingPage;
