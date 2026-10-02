import { createContext } from 'react';

/**
 * Theme context (dark "aurora night" ⇄ light "frosted daylight").
 * Kept in its own module so both the provider and the `useTheme` hook can
 * import it without tripping React fast-refresh boundaries.
 */
export const ThemeContext = createContext({
  theme: 'dark',
  isDark: true,
  setTheme: () => {},
  toggleTheme: () => {},
});
