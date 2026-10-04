// @vitest-environment jsdom
import React from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom/vitest';
import AskConsultantDialog from './AskConsultantDialog';

vi.mock('../../api/consultationApi', () => ({
  default: {
    getContextStatus: vi.fn().mockResolvedValue({ hasOpenRequest: false }),
    matchFaq: vi.fn().mockResolvedValue({ matches: [] }),
    create: vi.fn(),
    describeError: vi.fn((_error, fallback) => fallback),
  },
}));

const renderDialog = (props = {}) =>
  render(
    <MemoryRouter>
      <AskConsultantDialog {...props} />
    </MemoryRouter>
  );

afterEach(() => cleanup());

describe('AskConsultantDialog', () => {
  it('moves focus to the first input when it opens', async () => {
    renderDialog();
    const subject = await screen.findByLabelText('Your question in one line');
    await waitFor(() => expect(subject).toHaveFocus(), { timeout: 2000 });
  });

  it('locks page scroll while open and restores it on close', async () => {
    const onClose = vi.fn();
    renderDialog({ onClose });

    expect(document.body.style.overflow).toBe('hidden');
    fireEvent.keyDown(window, { key: 'Escape' });
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('keeps the dialog open for typing and submits through the form', async () => {
    const consultationApi = (await import('../../api/consultationApi')).default;
    consultationApi.create.mockResolvedValue({ id: 'c-1' });
    const onCreated = vi.fn();
    const onClose = vi.fn();
    renderDialog({ onCreated, onClose });

    const subject = await screen.findByLabelText('Your question in one line');
    fireEvent.change(subject, { target: { value: 'Which pathway fits my budget better?' } });
    fireEvent.change(screen.getByLabelText('Details'), {
      target: { value: 'Both look interesting but tuition costs differ a lot.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /send to a consultant/i }));

    await waitFor(() => expect(consultationApi.create).toHaveBeenCalled());
    expect(onCreated).toHaveBeenCalledWith({ id: 'c-1' });
    expect(onClose).toHaveBeenCalled();
  });
});
