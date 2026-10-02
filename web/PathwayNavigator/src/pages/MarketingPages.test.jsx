// @vitest-environment jsdom
import React from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom/vitest';
import HomePage from './HomePage';
import AboutPage from './AboutPage';
import ContactPage from './ContactPage';
import NotFoundPage from './NotFoundPage';
import { ThemeProvider } from '../context/ThemeContext';

const renderWithRouter = (ui) =>
  render(
    <ThemeProvider>
      <MemoryRouter>{ui}</MemoryRouter>
    </ThemeProvider>
  );

describe('marketing pages', () => {
  afterEach(() => cleanup());

  it('renders the home page hero, agent workflow and FAQ', () => {
    renderWithRouter(<HomePage />);

    expect(
      screen.getByRole('heading', { name: /Navigate your future with confidence/i })
    ).toBeInTheDocument();
    expect(screen.getByText('Agent 1')).toBeInTheDocument();
    expect(screen.getByText('Agent 4')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Start your journey/i })).toHaveAttribute('href', '/register');
    expect(screen.getByText(/Do I need perfect exam results/i)).toBeInTheDocument();
  });

  it('renders the about page principles and four agent cards', () => {
    renderWithRouter(<AboutPage />);

    expect(screen.getByRole('heading', { name: /Career guidance that shows its working/i })).toBeInTheDocument();
    expect(screen.getByText('No dead ends')).toBeInTheDocument();
    expect(screen.getAllByText('Agent 3').length).toBeGreaterThan(0);
  });

  it('validates the contact form before acknowledging a submission', async () => {
    renderWithRouter(<ContactPage />);

    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));
    expect(screen.getByText('Please tell us your name.')).toBeInTheDocument();
    expect(screen.getByText(/An email address is required/i)).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/Your name/i), { target: { value: 'Nimal Perera' } });
    fireEvent.change(screen.getByLabelText(/Email address/i), { target: { value: 'nimal@example.com' } });
    fireEvent.change(screen.getByLabelText(/Message/i), {
      target: { value: 'I would like to know more about the counsellor workflow.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Send message/i }));

    expect(await screen.findByText(/Thank you, Nimal/i)).toBeInTheDocument();
  });

  it('renders the 404 page with recovery links', () => {
    renderWithRouter(<NotFoundPage />);

    expect(screen.getByText('404')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Return home/i })).toHaveAttribute('href', '/');
  });
});
