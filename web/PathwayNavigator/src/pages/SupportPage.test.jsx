// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import SupportPage from './SupportPage';
import consultationApi from '../api/consultationApi';

vi.mock('../api/consultationApi', () => ({
  default: {
    create: vi.fn(),
    getMine: vi.fn(),
    getById: vi.fn(),
    addMessage: vi.fn(),
    close: vi.fn(),
    reopen: vi.fn(),
    matchFaq: vi.fn(),
  },
}));

const consultation = {
  id: 'c9',
  subject: 'Is the AI Engineer roadmap too expensive?',
  contextType: 'PathwayPlan',
  contextSummary: 'Pathway plan · AI Engineer',
  status: 'Answered',
  priority: 'P2',
  isSlaBreached: false,
  closedAt: null,
  assignedConsultantName: 'Ms. Perera',
  consultantGuidanceJson: '{"note":"Apply for the merit scholarship first.","checklist":["Submit the scholarship form"]}',
  messages: [
    { id: 'm1', authorName: 'You', authorRole: 'Student', body: 'Is there a cheaper route?', isInternal: false, createdAt: '2026-10-03T08:00:00Z', resources: [] },
    { id: 'm2', authorName: 'Ms. Perera', authorRole: 'Consultant', body: 'Yes - the merit scholarship covers it.', isInternal: false, createdAt: '2026-10-03T09:00:00Z', resources: [{ label: 'Scholarship guide', url: 'https://example.edu/scholarships' }] },
  ],
};

describe('SupportPage', () => {
  afterEach(() => cleanup());

  beforeEach(() => {
    vi.clearAllMocks();
    consultationApi.getMine.mockResolvedValue({ items: [consultation], totalCount: 1, totalPages: 1 });
    consultationApi.getById.mockResolvedValue(consultation);
    consultationApi.addMessage.mockResolvedValue(consultation);
    consultationApi.reopen.mockResolvedValue({ ...consultation, status: 'InProgress' });
  });

  it('lists the student questions with who answered them', async () => {
    render(<MemoryRouter><SupportPage /></MemoryRouter>);

    expect(await screen.findByText('Is the AI Engineer roadmap too expensive?')).toBeInTheDocument();
    expect(screen.getByText('Ms. Perera')).toBeInTheDocument();
  });

  it('opens the notification deep link straight onto that conversation', async () => {
    render(
      <MemoryRouter initialEntries={['/student/support?consultation=c9']}>
        <SupportPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Yes - the merit scholarship covers it.')).toBeInTheDocument();
    expect(consultationApi.getById).toHaveBeenCalledWith('c9');
    // the guidance written back by the consultant is visible in the thread
    expect(screen.getByLabelText('Consultant guidance')).toHaveTextContent('Apply for the merit scholarship first.');
    expect(screen.getByRole('link', { name: /Scholarship guide/ })).toHaveAttribute('href', 'https://example.edu/scholarships');
  });

  it('lets the student continue the conversation after the answer', async () => {
    render(
      <MemoryRouter initialEntries={['/student/support?consultation=c9']}>
        <SupportPage />
      </MemoryRouter>,
    );
    await screen.findByText('Yes - the merit scholarship covers it.');

    fireEvent.change(screen.getByLabelText('Continue the conversation'), { target: { value: 'Thank you, I have submitted it.' } });
    fireEvent.submit(screen.getByLabelText('Continue the conversation'));

    await waitFor(() => expect(consultationApi.addMessage).toHaveBeenCalledWith('c9', 'Thank you, I have submitted it.'));
  });

  it('shows the empty state when nothing has been asked yet', async () => {
    consultationApi.getMine.mockResolvedValue({ items: [], totalCount: 0, totalPages: 0 });
    render(<MemoryRouter><SupportPage /></MemoryRouter>);

    expect(await screen.findByText(/No questions yet/)).toBeInTheDocument();
  });
});
