// @vitest-environment jsdom
import React from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import ConsultantNote from './ConsultantNote';

describe('ConsultantNote', () => {
  afterEach(() => cleanup());

  it('renders the guidance list written back onto a pathway milestone', () => {
    render(<ConsultantNote guidance={JSON.stringify([
      { stageKey: 'skills', note: 'Start with the free Python course before paying for anything.', checklist: ['Finish module 1'], consultantName: 'Mr. Silva', createdAt: '2026-10-02T00:00:00Z' },
    ])} />);

    expect(screen.getByText('Start with the free Python course before paying for anything.')).toBeInTheDocument();
    expect(screen.getByText('Finish module 1')).toBeInTheDocument();
    expect(screen.getByText(/Mr. Silva/)).toBeInTheDocument();
  });

  it('accepts an already-parsed object as well as the stored string', () => {
    render(<ConsultantNote guidance={{ note: 'Book the lab session on Tuesday.' }} title="Consultant help with this review" />);

    expect(screen.getByText('Book the lab session on Tuesday.')).toBeInTheDocument();
    expect(screen.getByLabelText('Consultant help with this review')).toBeInTheDocument();
  });

  it('renders nothing when there is no guidance (or it is unreadable)', () => {
    const { container, rerender } = render(<ConsultantNote guidance="not json at all" />);
    expect(container).toBeEmptyDOMElement();

    rerender(<ConsultantNote guidance="[]" />);
    expect(container).toBeEmptyDOMElement();

    rerender(<ConsultantNote guidance={'{"stageKey":"skills","note":null,"resources":[]}'} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('links resources with a safe external rel', () => {
    render(<ConsultantNote guidance={JSON.stringify({ note: 'See this guide.', resources: [{ label: 'AL stream guide', url: 'https://example.edu/streams' }] })} />);

    expect(screen.getByRole('link', { name: /AL stream guide/ })).toHaveAttribute('rel', 'noreferrer noopener');
  });
});
