/**
 * Small feature flags derived from the Vite environment.
 *
 * Google Sign-In is optional. When VITE_GOOGLE_CLIENT_ID is missing the auth pages must render the
 * email/password form only - previously the "or sign in with email" divider was still shown without
 * a button above it, and the "not configured" warning was logged on every single render.
 */
export const googleClientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID || '').trim();

export const isGoogleSignInEnabled = googleClientId.length > 0;
