// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import StandardOnboardingForm from './StandardOnboardingForm';

vi.mock('../../api/onboardingApi', () => ({ submitStandardOnboardingApi: vi.fn() }));
vi.mock('../../api/profileApi', () => ({ updateStudentProfileApi: vi.fn() }));

const { submitStandardOnboardingApi } = await import('../../api/onboardingApi');
const { updateStudentProfileApi } = await import('../../api/profileApi');

const savedProfile = {
  email: 'nimal@example.com',
  fullName: 'Nimal Perera',
  academicStage: 'Undergraduate',
  coreSkills: ['Python', 'SQL'],
  hobbiesInterests: ['AI'],
  careerAmbitions: 'AI Engineer',
};

const renderForm = (props) =>
  render(
    <MemoryRouter>
      <StandardOnboardingForm {...props} />
    </MemoryRouter>
  );

beforeEach(() => {
  vi.clearAllMocks();
  submitStandardOnboardingApi.mockResolvedValue({});
  updateStudentProfileApi.mockResolvedValue({});
});

afterEach(() => cleanup());

describe('StandardOnboardingForm', () => {
  it('pre-fills the saved profile in update mode, including the full name', () => {
    renderForm({ mode: 'update', initialProfile: savedProfile });

    expect(screen.getByLabelText(/Full Name/i).value).toBe('Nimal Perera');
    expect(screen.getByLabelText(/Career Ambitions/i).value).toBe('AI Engineer');
    expect(screen.getByText('Python')).toBeTruthy();
    expect(screen.getByText('SQL')).toBeTruthy();
    expect(screen.getByRole('button', { name: /Save Profile Changes/i })).toBeTruthy();
  });

  it('saves edits through the partial profile update endpoint', async () => {
    renderForm({ mode: 'update', initialProfile: savedProfile });

    fireEvent.change(screen.getByLabelText(/Full Name/i), { target: { value: 'Nimal Jayasuriya' } });
    fireEvent.change(screen.getByLabelText(/Career Ambitions/i), { target: { value: 'Data Scientist' } });
    fireEvent.click(screen.getByRole('button', { name: /Save Profile Changes/i }));

    await waitFor(() => expect(updateStudentProfileApi).toHaveBeenCalledTimes(1));
    expect(updateStudentProfileApi).toHaveBeenCalledWith({
      fullName: 'Nimal Jayasuriya',
      academicStage: 'Undergraduate',
      coreSkills: ['Python', 'SQL'],
      hobbiesInterests: ['AI'],
      careerAmbitions: 'Data Scientist',
    });
    // The onboarding endpoint must not be used for an update.
    expect(submitStandardOnboardingApi).not.toHaveBeenCalled();
  });

  it('requires a full name', async () => {
    renderForm({ mode: 'update', initialProfile: { ...savedProfile, fullName: '' } });

    fireEvent.click(screen.getByRole('button', { name: /Save Profile Changes/i }));

    await waitFor(() => expect(screen.getByText(/tell us your full name/i)).toBeTruthy());
    expect(updateStudentProfileApi).not.toHaveBeenCalled();
  });

  it('still completes first-time onboarding through the onboarding endpoint', async () => {
    renderForm({ mode: 'create', initialProfile: null });

    fireEvent.change(screen.getByLabelText(/Full Name/i), { target: { value: 'Amaya Silva' } });
    fireEvent.change(screen.getByLabelText(/Career Ambitions/i), { target: { value: 'AI Engineer' } });
    fireEvent.click(screen.getByRole('button', { name: /Save & Access Dashboard/i }));

    await waitFor(() => expect(submitStandardOnboardingApi).toHaveBeenCalledTimes(1));
    expect(submitStandardOnboardingApi).toHaveBeenCalledWith(
      expect.objectContaining({ fullName: 'Amaya Silva', onboardingMethod: 'StandardForm' })
    );
    expect(updateStudentProfileApi).not.toHaveBeenCalled();
  });
});
