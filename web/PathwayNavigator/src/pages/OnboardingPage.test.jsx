// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import OnboardingPage from './OnboardingPage';

vi.mock('../api/profileApi', () => ({
  getProfileStatusApi: vi.fn(),
  getStudentProfileApi: vi.fn(),
}));

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({ user: { email: 'nimal@example.com' }, isAuthenticated: true }),
}));

// The chat window has its own agent/network behaviour; here we only care about what the page
// loads into it.
vi.mock('../components/onboarding/AgentChatWindow', () => ({
  default: ({ mode, currentSlots, baselineSlots }) => (
    <div data-testid="chat">
      {`mode=${mode};name=${currentSlots?.full_name};skills=${(currentSlots?.core_skills || []).join('|')};baseline=${baselineSlots ? 'yes' : 'no'}`}
    </div>
  ),
}));

vi.mock('../components/onboarding/StandardOnboardingForm', () => ({
  default: ({ mode, initialProfile }) => (
    <div data-testid="form">{`mode=${mode};name=${initialProfile?.fullName ?? 'none'}`}</div>
  ),
}));

const { getProfileStatusApi, getStudentProfileApi } = await import('../api/profileApi');

const savedProfile = {
  id: 'p1',
  userId: 'u1',
  email: 'nimal@example.com',
  fullName: 'Nimal Perera',
  academicStage: 'Undergraduate',
  coreSkills: ['Python', 'SQL'],
  hobbiesInterests: ['AI'],
  careerAmbitions: 'AI Engineer',
  updatedAt: '2026-09-30T10:00:00Z',
};

const renderPage = (initialPath = '/onboarding') =>
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="/onboarding" element={<OnboardingPage />} />
        <Route path="/dashboard" element={<div data-testid="dashboard">Dashboard</div>} />
      </Routes>
    </MemoryRouter>
  );

beforeEach(() => {
  vi.clearAllMocks();
  getProfileStatusApi.mockResolvedValue({ hasProfile: true, isOnboardingCompleted: true, academicStage: 'Undergraduate' });
  getStudentProfileApi.mockResolvedValue(savedProfile);
});

afterEach(() => {
  cleanup();
});

describe('OnboardingPage', () => {
  it('turns the plain onboarding route into a profile update for a student who already finished', async () => {
    // Regression: this used to silently bounce back to the dashboard, which made every
    // "AI Onboarding" link (navbar, footer, dashboard tile) look broken.
    renderPage('/onboarding');

    await waitFor(() => expect(screen.getByTestId('chat')).toBeTruthy());
    expect(screen.queryByTestId('dashboard')).toBeNull();
    expect(screen.getByTestId('chat').textContent).toContain('mode=update');
    expect(getStudentProfileApi).toHaveBeenCalledTimes(1);
  });

  it('keeps first-time onboarding in create mode', async () => {
    getProfileStatusApi.mockResolvedValue({ hasProfile: false, isOnboardingCompleted: false, academicStage: null });
    getStudentProfileApi.mockResolvedValue(null);

    renderPage('/onboarding');

    await waitFor(() => expect(screen.getByTestId('chat')).toBeTruthy());
    expect(screen.getByTestId('chat').textContent).toBe('mode=create;name=null;skills=;baseline=no');
  });

  it('stays on the page and loads the saved profile in update mode (Re-run AI onboarding)', async () => {
    renderPage('/onboarding?mode=update');

    await waitFor(() => expect(screen.getByTestId('chat')).toBeTruthy());
    expect(screen.queryByTestId('dashboard')).toBeNull();
    expect(screen.getByTestId('chat').textContent).toBe(
      'mode=update;name=Nimal Perera;skills=Python|SQL;baseline=yes'
    );
    // The profile is loaded once, not re-fetched on every render.
    expect(getStudentProfileApi).toHaveBeenCalledTimes(1);
  });

  it('renders the update copy and keeps the standard form pre-filled', async () => {
    renderPage('/onboarding?mode=update');

    await waitFor(() => expect(screen.getByText(/keep your profile up to date/i)).toBeTruthy());
    expect(screen.getByTestId('chat').textContent).toContain('mode=update');
  });

  it('still works when the student has no saved profile yet', async () => {
    getStudentProfileApi.mockRejectedValue(new Error('Failed to fetch student profile.'));
    getProfileStatusApi.mockResolvedValue({ hasProfile: false, isOnboardingCompleted: false, academicStage: null });

    renderPage('/onboarding?mode=update');

    await waitFor(() => expect(screen.getByTestId('chat')).toBeTruthy());
    expect(screen.getByTestId('chat').textContent).toContain('name=null');
  });
});
