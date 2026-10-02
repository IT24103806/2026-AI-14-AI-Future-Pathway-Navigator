/**
 * Extracts a human-readable message from an axios error produced by the ASP.NET Core API.
 *
 * The backend answers with one of:
 *   - { message: "..." }                                      (controller-level business errors, 4xx)
 *   - ProblemDetails { title, detail, errors: { Field: [] } }  (global handler / model validation)
 *   - a plain string
 *
 * Two rules matter here:
 *   1. Never render a server dump. In Development ASP.NET Core answers unhandled errors with an HTML
 *      developer exception page (stack trace, request headers, connection error). That text used to
 *      be shown verbatim inside the app's error banner, which made one transient database blip look
 *      like a broken application.
 *   2. A 5xx means the server failed, not that the user typed something wrong, so it must never get
 *      the caller's context-specific fallback ("Invalid email or password.").
 */

export const NETWORK_ERROR_MESSAGE = 'Network error. Unable to reach the Pathway Navigator API.';
export const TIMEOUT_MESSAGE = 'The server took too long to respond. The AI service may be busy — please try again.';
export const SERVER_ERROR_MESSAGE =
  'The server hit a temporary problem and your request was not completed. Please try again in a moment.';

/** Signatures of an ASP.NET Core developer exception page, a .NET stack trace or raw HTML. */
const looksLikeServerDump = (text) =>
  /<!doctype html|<html[\s>]|<head>|<body>/i.test(text) ||
  // A .NET stack frame, e.g. "   at PathwayNavigator.Api.Services.AuthService.LoginAsync(...)"
  /\bat\s+[\w.<>`+]+\(/i.test(text) ||
  /(Npgsql|Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|System\.(InvalidOperationException|IO\.|Net\.|Threading))/.test(text) ||
  /HEADERS\s*={2,}/i.test(text);

/** Trims a candidate message, drops server dumps and caps how much text the UI can receive. */
const cleanMessage = (value) => {
  if (typeof value !== 'string') return null;
  const text = value.trim();
  if (!text || looksLikeServerDump(text)) return null;
  return text.length > 300 ? `${text.slice(0, 297)}…` : text;
};

/** True for the failures that are worth retrying: the API was momentarily unreachable. */
export const isTransientApiError = (error) => {
  const status = error?.response?.status;
  if (status) return status === 502 || status === 503 || status === 504;
  return !error?.response && error?.code !== 'ECONNABORTED';
};

export const getApiErrorMessage = (error, fallback = 'Something went wrong. Please try again.') => {
  const response = error?.response;
  if (!response) {
    if (error?.code === 'ECONNABORTED') return TIMEOUT_MESSAGE;
    return NETWORK_ERROR_MESSAGE;
  }

  const status = response.status;
  const data = response.data;

  // A plain-text/HTML body: keep it only when it is a real message, never a stack trace.
  const textBody = cleanMessage(data);
  if (textBody) return textBody;

  const message = cleanMessage(data?.message);
  if (message) return message;

  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors)
      .flat()
      .filter((entry) => typeof entry === 'string' && entry.trim());
    if (messages.length > 0) return cleanMessage(messages.join(' '));
  }

  // The server broke, the client did not misbehave - always say so with an actionable message.
  if (status >= 500) return SERVER_ERROR_MESSAGE;

  if (status === 401) return 'Your session has expired. Please sign in again.';
  if (status === 403) return 'You do not have permission to perform this action.';

  return cleanMessage(data?.title) || fallback;
};
