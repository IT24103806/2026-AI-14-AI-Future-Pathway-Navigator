import { createContext } from 'react';

/**
 * Kept in its own module (like AuthContextDefinition) so the provider file only exports components -
 * that keeps fast-refresh happy and avoids the "only-export-components" lint warning.
 */
export const NotificationContext = createContext(null);
