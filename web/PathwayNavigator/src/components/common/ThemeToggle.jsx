import React from 'react';
import { useTheme } from '../../hooks/useTheme';

/**
 * Segmented dark/light switch. Purely presentational — the actual theme state
 * lives in ThemeContext so every page (and the CSS token layer) stays in sync.
 */
const ThemeToggle = ({ className = '' }) => {
  const { theme, setTheme } = useTheme();
  const isDark = theme === 'dark';

  return (
    <button
      type="button"
      className={`theme-toggle ${className}`.trim()}
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      role="switch"
      aria-checked={!isDark}
      aria-label={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
      title={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
    >
      <span className={`theme-toggle__option ${isDark ? 'is-active' : ''}`} aria-hidden="true">🌙</span>
      <span className={`theme-toggle__option ${!isDark ? 'is-active' : ''}`} aria-hidden="true">☀️</span>
    </button>
  );
};

export default ThemeToggle;
