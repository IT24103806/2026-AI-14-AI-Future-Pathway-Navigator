// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import ConsultantDashboardPage from './ConsultantDashboardPage';
import consultantApi from '../api/consultantApi';

vi.mock('../api/consultantApi', () => ({
  default: {
    getQueue: vi.fn(),
    getCase: vi.fn(),
    getBrief: vi.fn(),
    getDraft: vi.fn(),
    getStats: vi.fn(),
    claim: vi.fn(),
    release: vi.fn(),
    reply: vi.fn(),
    addNote: vi.fn(),
    updatePriority: vi.fn(),
    escalate: vi.fn(),
  },
}));

const consultation = {
  id: 'c1',
  subject: 'Which pathway fits my budget?',
  studentName: 'Nimal',
  contextType: 'CareerDiscovery',
  contextSummary: 'Career Discovery · top matches: AI Engineer',
  contextSnapshotJson: '{"topCareers":["AI Engineer"],"status":"pending_approval"}',
  category: 'PathwayAdvice',
  priority: 'P2',
  status: 'Open',
  body: 'AI Engineer looks expensive for my family. Is there a cheaper route?',
  slaDueAt: new Date(Date.now() + 36e5 * 20).toISOString(),
  isSlaBreached: false,
  closedAt: null,
  assignedConsultantId: null,
  assignedConsultantName: null,
  consultantGuidanceJson: '{}',
  messages: [
    { id: 'm1', authorName: 'Nimal', authorRole: 'Student', body: 'AI Engineer looks expensive.', isInternal: false, createdAt: '2026-10-03T08:00:00Z', resources: [] },
    { id: 'm2', authorName: 'Staff (internal)', authorRole: 'Consultant', body: 'Check the scholarship page.', isInternal: true, createdAt: '2026-10-03T08:10:00Z', resources: [] },
  ],
  // snake_case on purpose: AgentTriageResultDto is serialised with its [JsonPropertyName] attributes
  agentTriage: { category: 'PathwayAdvice', priority: 'P2', expertise_tags: ['AI/ML'], language: 'English', sentiment: 'concerned', safety_flags: [], suggested_sla_hours: 48, confidence: 0.8 },
};

const renderDesk = () => render(
  <MemoryRouter>
    <ConsultantDashboardPage />
  </MemoryRouter>,
);

describe('ConsultantDashboardPage', () => {
  afterEach(() => cleanup());

  beforeEach(() => {
    vi.clearAllMocks();
    consultantApi.getQueue.mockResolvedValue({ items: [consultation], totalCount: 1, totalPages: 1 });
    consultantApi.getCase.mockResolvedValue(consultation);
    consultantApi.getStats.mockResolvedValue({
      unclaimedPool: 1, assignedToMe: 0, slaCompliancePercent: 90, averageRating: 4.5, openCases: 1,
    });
    consultantApi.getBrief.mockResolvedValue({
      headline: 'PathwayAdvice · CareerDiscovery',
      confidence: 0.7,
      sections: [{ title: 'What the student is asking', points: ['AI Engineer looks expensive'] }],
    });
    consultantApi.getDraft.mockResolvedValue({
      draftId: 'd1', body: 'Here is a cheaper accredited route…', citations: ['profile'], confidence: 0.8,
      lowConfidence: false, safetyNotes: [], mustEscalate: false, humanReviewRequired: true,
    });
    consultantApi.reply.mockResolvedValue({ ...consultation, status: 'Answered' });
    consultantApi.claim.mockResolvedValue({ ...consultation, assignedConsultantId: 'me', status: 'Claimed' });
  });

  it('loads the queue and shows the frozen student context when a case is opened', async () => {
    renderDesk();
    fireEvent.click(await screen.findByText('Which pathway fits my budget?'));

    expect(await screen.findByText(/AI Engineer looks expensive for my family/)).toBeInTheDocument();
    // the frozen snapshot is rendered key by key next to the server summary
    expect(await screen.findByText('topCareers')).toBeInTheDocument();
    expect(screen.getAllByText('Career Discovery · top matches: AI Engineer').length).toBeGreaterThan(0);
    // Agent 5 brief is rendered as guidance for the human, not as an automatic answer.
    expect(await screen.findByText('What the student is asking')).toBeInTheDocument();

    // the triage evidence uses the raw wire field names the DTO actually serialises
    fireEvent.click(screen.getByText('Agent 5 triage evidence'));
    expect(screen.getByText(/Expertise: AI\/ML/)).toBeInTheDocument();
    expect(screen.getByText(/SLA 48h/)).toBeInTheDocument();
  });

  it('inserts an Agent 5 draft but still requires the consultant to send it', async () => {
    renderDesk();
    fireEvent.click(await screen.findByText('Which pathway fits my budget?'));
    await screen.findByText(/AI Engineer looks expensive for my family/);

    fireEvent.click(screen.getByText('Supportive'));
    await waitFor(() => expect(consultantApi.getDraft).toHaveBeenCalledWith('c1', 'Supportive'));

    const replyBox = await screen.findByLabelText('Your reply');
    await waitFor(() => expect(replyBox.value).toContain('cheaper accredited route'));

    // Nothing is sent by inserting a draft - the human still presses send.
    expect(consultantApi.reply).not.toHaveBeenCalled();

    fireEvent.click(screen.getByText('Send reply to student'));
    await waitFor(() => expect(consultantApi.reply).toHaveBeenCalledTimes(1));
    expect(consultantApi.reply.mock.calls[0][1].message).toContain('cheaper accredited route');
    expect(consultantApi.reply.mock.calls[0][1].usedAgentDraft).toBe(true);
  });

  it('refuses to send an empty reply', async () => {
    renderDesk();
    fireEvent.click(await screen.findByText('Which pathway fits my budget?'));
    await screen.findByText(/AI Engineer looks expensive for my family/);

    fireEvent.click(screen.getByText('Send reply to student'));

    expect(await screen.findByRole('alert')).toHaveTextContent('Write a reply before sending');
    expect(consultantApi.reply).not.toHaveBeenCalled();
  });

  it('keeps internal notes out of the student-facing conversation view', async () => {
    renderDesk();
    fireEvent.click(await screen.findByText('Which pathway fits my budget?'));
    await screen.findByText(/AI Engineer looks expensive for my family/);

    fireEvent.click(screen.getByText('Internal notes'));
    expect(await screen.findByText('Check the scholarship page.')).toBeInTheDocument();
    expect(screen.getByText(/never shown to the student/)).toBeInTheDocument();
  });
});
