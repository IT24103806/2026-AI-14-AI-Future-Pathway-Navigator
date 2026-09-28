// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import CounsellorDashboardPage from './CounsellorDashboardPage';
import { counsellorReviewApi } from '../api/counsellorReviewApi';

vi.mock('../api/counsellorReviewApi', () => ({
  counsellorReviewApi: { getReviews: vi.fn(), getReviewById: vi.fn(), submitDecision: vi.fn() },
}));

const review = { id: 'r1', targetCareer: 'AI Engineer', workflowId: 'wf-1', status: 'Pending', feasibilityScore: 54,
  isHighRisk: true, riskReason: 'Stream mismatch', feasibilitySummary: 'Two gaps', missingSkillsJson: '["Python"]',
  validationResultsJson: '["request_schema_valid"]', toolCallsJson: '[]', executionTraceJson: '[]', createdAt: '2026-09-23T00:00:00Z' };

describe('CounsellorDashboardPage', () => {
  afterEach(() => cleanup());
  beforeEach(() => { vi.clearAllMocks(); counsellorReviewApi.getReviews.mockResolvedValue({ items: [review], totalCount: 1, totalPages: 1 }); counsellorReviewApi.getReviewById.mockResolvedValue(review); });
  it('renders risk evidence and requires meaningful feedback', async () => {
    render(<CounsellorDashboardPage />);
    fireEvent.click(await screen.findByText('AI Engineer'));
    expect(await screen.findByText('Stream mismatch')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Approve'));
    expect(screen.getByRole('alert')).toHaveTextContent('at least 5 characters');
    expect(counsellorReviewApi.submitDecision).not.toHaveBeenCalled();
  });
  it('submits a decision and refreshes the queue', async () => {
    counsellorReviewApi.submitDecision.mockResolvedValue({ ...review, status: 'Approved' });
    render(<CounsellorDashboardPage />);
    fireEvent.click(await screen.findByText('AI Engineer'));
    await screen.findByText('Stream mismatch');
    fireEvent.change(screen.getByLabelText('Required counsellor feedback'), { target: { value: 'Meets requirements after review.' } });
    fireEvent.click(screen.getByText('Approve'));
    await waitFor(() => expect(counsellorReviewApi.submitDecision).toHaveBeenCalledWith('r1', 'Approved', 'Meets requirements after review.'));
  });
});
