// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom/vitest';
import StudentRealityCheckPage from './StudentRealityCheckPage';
import { counsellorReviewApi } from '../api/counsellorReviewApi';

vi.mock('../api/counsellorReviewApi', () => ({
  counsellorReviewApi: {
    getMyStatus: vi.fn(),
    getMyHistory: vi.fn(),
    resubmitRealityCheck: vi.fn(),
    startRealityCheck: vi.fn(),
  },
}));

vi.mock('../api/profileApi', () => ({
  getStudentProfileApi: vi.fn().mockResolvedValue(null),
}));

vi.mock('../api/pathwayApi', () => ({
  getLatestPathwayAnalysisApi: vi.fn().mockResolvedValue(null),
}));

const result = {
  id: 'r1',
  pathwayAnalysisId: 'a1',
  targetCareer: 'AI Engineer',
  alStream: 'Physical Science',
  alResults: 'A, B, C',
  budgetLevel: 'Medium',
  currentSkillsJson: '["Python"]',
  workflowId: 'wf-4',
  status: 'Approved',
  feasibilityScore: 72,
  feasibilitySummary: 'Strong match with two gaps.',
  isHighRisk: true,
  riskReason: 'Mathematics prerequisite needs verification.',
  degreeRequirement: 'Computing degree',
  subjectRequirementsJson: '["Mathematics"]',
  entryRequirementsJson: '["Three A/L passes"]',
  costGuidance: 'Compare tuition and travel costs.',
  missingSkillsJson: '["Statistics"]',
  gapClosurePlanJson: '["Complete a Statistics foundation course"]',
  counsellorFeedback: 'Proceed after verification.',
  createdAt: '2026-09-23T00:00:00Z',
};

describe('StudentRealityCheckPage', () => {
  afterEach(() => cleanup());
  beforeEach(() => {
    vi.clearAllMocks();
    counsellorReviewApi.getMyHistory.mockResolvedValue([]);
  });

  it('renders the student Agent 4 evidence and counsellor decision', async () => {
    counsellorReviewApi.getMyStatus.mockResolvedValue(result);
    render(
      <MemoryRouter>
        <StudentRealityCheckPage />
      </MemoryRouter>
    );
    expect(await screen.findByRole('heading', { name: 'AI Engineer' })).toBeInTheDocument();
    expect(screen.getByText('Strong match with two gaps.')).toBeInTheDocument();
    expect(screen.getByText('Statistics')).toBeInTheDocument();
    expect(screen.getByText('Proceed after verification.')).toBeInTheDocument();
    expect(screen.getByText(/🎓 Stream: Physical Science/)).toBeInTheDocument();
  });

  it('shows a guided empty state before an Agent 4 run exists', async () => {
    counsellorReviewApi.getMyStatus.mockRejectedValue({ response: { status: 404 } });
    render(
      <MemoryRouter>
        <StudentRealityCheckPage />
      </MemoryRouter>
    );
    expect(await screen.findByRole('heading', { name: 'No Reality Check yet' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Choose a career pathway' })).toHaveAttribute(
      'href',
      '/career-discovery'
    );
  });

  it('automatically opens the pre-filled Revise & Resubmit form when NeedsRevision and allows adding missing skills', async () => {
    const needsRevisionResult = {
      ...result,
      status: 'NeedsRevision',
      counsellorFeedback: 'Complete Statistics fundamentals and resubmit.',
    };
    const resubmittedResult = {
      ...result,
      id: 'r2',
      status: 'Approved',
      feasibilityScore: 90,
      currentSkillsJson: '["Python","Statistics"]',
      missingSkillsJson: '[]',
    };
    counsellorReviewApi.getMyStatus.mockResolvedValue(needsRevisionResult);
    counsellorReviewApi.getMyHistory.mockResolvedValue([needsRevisionResult]);
    counsellorReviewApi.resubmitRealityCheck.mockResolvedValue(resubmittedResult);

    render(
      <MemoryRouter>
        <StudentRealityCheckPage />
      </MemoryRouter>
    );

    expect(
      await screen.findByRole('heading', { name: /Revise & Resubmit Reality Check/i })
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue('Physical Science')).toBeInTheDocument();
    expect(screen.getByDisplayValue('A, B, C')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Python')).toBeInTheDocument();

    // Quick-add the missing skill via chip
    fireEvent.click(screen.getByRole('button', { name: '+ Add Statistics' }));
    expect(screen.getByDisplayValue('Python, Statistics')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Resubmit Reality Check' }));

    await waitFor(() => {
      expect(counsellorReviewApi.resubmitRealityCheck).toHaveBeenCalledWith('r1', {
        targetCareer: 'AI Engineer',
        alStream: 'Physical Science',
        alResults: 'A, B, C',
        budgetLevel: 'Medium',
        currentSkills: ['Python', 'Statistics'],
      });
    });

    expect(
      await screen.findByText(/Updated Reality Check submitted! New status: Approved/i)
    ).toBeInTheDocument();
  });
});
