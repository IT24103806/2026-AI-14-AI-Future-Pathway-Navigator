// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom/vitest';
import StudentRealityCheckPage from './StudentRealityCheckPage';
import { counsellorReviewApi } from '../api/counsellorReviewApi';

vi.mock('../api/counsellorReviewApi', () => ({
  counsellorReviewApi: { getMyStatus: vi.fn(), getMyHistory: vi.fn() },
}));

const result = {
  id: 'r1', targetCareer: 'AI Engineer', workflowId: 'wf-4', status: 'Approved',
  feasibilityScore: 72, feasibilitySummary: 'Strong match with two gaps.', isHighRisk: true,
  riskReason: 'Mathematics prerequisite needs verification.', degreeRequirement: 'Computing degree',
  subjectRequirementsJson: '["Mathematics"]', entryRequirementsJson: '["Three A/L passes"]',
  costGuidance: 'Compare tuition and travel costs.', missingSkillsJson: '["Python"]',
  gapClosurePlanJson: '["Complete a Python foundation course"]', counsellorFeedback: 'Proceed after verification.',
  createdAt: '2026-09-23T00:00:00Z',
};

describe('StudentRealityCheckPage', () => {
  afterEach(() => cleanup());
  beforeEach(() => { vi.clearAllMocks(); counsellorReviewApi.getMyHistory.mockResolvedValue([]); });

  it('renders the student Agent 4 evidence and counsellor decision', async () => {
    counsellorReviewApi.getMyStatus.mockResolvedValue(result);
    render(<MemoryRouter><StudentRealityCheckPage /></MemoryRouter>);
    expect(await screen.findByRole('heading', { name: 'AI Engineer' })).toBeInTheDocument();
    expect(screen.getByText('Strong match with two gaps.')).toBeInTheDocument();
    expect(screen.getByText('Python')).toBeInTheDocument();
    expect(screen.getByText('Proceed after verification.')).toBeInTheDocument();
  });

  it('shows a guided empty state before an Agent 4 run exists', async () => {
    counsellorReviewApi.getMyStatus.mockRejectedValue({ response: { status: 404 } });
    render(<MemoryRouter><StudentRealityCheckPage /></MemoryRouter>);
    expect(await screen.findByRole('heading', { name: 'No Reality Check yet' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Choose a career pathway' })).toHaveAttribute('href', '/career-discovery');
  });
});
