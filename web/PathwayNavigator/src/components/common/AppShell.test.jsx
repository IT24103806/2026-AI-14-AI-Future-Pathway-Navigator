// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom/vitest';
import Navbar from './Navbar';
import Footer from './Footer';
import { AuthProvider } from '../../context/AuthContext';
import { ThemeProvider } from '../../context/ThemeContext';

const renderShell = (ui) =>
  render(
    <ThemeProvider>
      <AuthProvider>
        <MemoryRouter>{ui}</MemoryRouter>
      </AuthProvider>
    </ThemeProvider>
  );

describe('app shell', () => {
  beforeEach(() => {
    window.localStorage.clear();
    document.documentElement.dataset.theme = 'dark';
  });

  afterEach(() => cleanup());

  it('renders the glass header for guests with auth links and a theme switch', () => {
    renderShell(<Navbar />);

    expect(screen.getByLabelText('PathwayNavigator home')).toHaveAttribute('href', '/');
    // Desktop header and mobile drawer both expose the auth actions.
    expect(screen.getAllByRole('link', { name: 'Login' })[0]).toHaveAttribute('href', '/login');
    expect(screen.getAllByRole('link', { name: 'Register' })[0]).toHaveAttribute('href', '/register');
    expect(screen.getByRole('switch')).toHaveAttribute('aria-checked', 'false');
  });

  it('toggles the document theme and persists the choice', () => {
    renderShell(<Navbar />);

    fireEvent.click(screen.getByRole('switch'));

    expect(document.documentElement.dataset.theme).toBe('light');
    expect(window.localStorage.getItem('pathwaynavigator-theme')).toBe('light');

    fireEvent.click(screen.getByRole('switch'));

    expect(document.documentElement.dataset.theme).toBe('dark');
    expect(window.localStorage.getItem('pathwaynavigator-theme')).toBe('dark');
  });

  it('exposes the primary public navigation in the footer', () => {
    renderShell(<Footer />);

    expect(screen.getByRole('navigation', { name: 'Company' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Contact Us' })).toHaveAttribute('href', '/contact');
    expect(screen.getByRole('link', { name: 'About Us' })).toHaveAttribute('href', '/about');
    expect(screen.getByText(/All rights reserved/i)).toBeInTheDocument();
  });
});
