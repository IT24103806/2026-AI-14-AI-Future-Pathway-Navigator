import { describe, expect, it } from 'vitest';
import { getApiErrorMessage, isTransientApiError, SERVER_ERROR_MESSAGE } from './apiError';

describe('getApiErrorMessage', () => {
  it('prefers the { message } body produced by the controllers', () => {
    expect(getApiErrorMessage({ response: { status: 409, data: { message: 'Already pending.' } } })).toBe('Already pending.');
  });

  it('flattens ASP.NET ProblemDetails validation errors', () => {
    const error = { response: { status: 400, data: { title: 'One or more validation errors occurred.', errors: { Password: ['Password must be at least 8 characters long.'], Email: ['Invalid email.'] } } } };
    expect(getApiErrorMessage(error)).toBe('Password must be at least 8 characters long. Invalid email.');
  });

  it('describes timeouts and network failures', () => {
    expect(getApiErrorMessage({ code: 'ECONNABORTED' })).toMatch(/too long/);
    expect(getApiErrorMessage({})).toMatch(/Network error/);
  });

  it('falls back to the caller message for client errors', () => {
    expect(getApiErrorMessage({ response: { status: 400, data: {} } }, 'Fallback.')).toBe('Fallback.');
    expect(getApiErrorMessage({ response: { status: 418, data: {} } }, 'Fallback.')).toBe('Fallback.');
  });

  it('blames the server, not the caller, for 5xx responses', () => {
    // Regression: a transient Npgsql failure used to surface as "Invalid email or password."
    expect(getApiErrorMessage({ response: { status: 500, data: {} } }, 'Invalid email or password.')).toBe(SERVER_ERROR_MESSAGE);
    expect(getApiErrorMessage({ response: { status: 503, data: { title: 'Service Unavailable' } } })).toBe(SERVER_ERROR_MESSAGE);
    expect(isTransientApiError({ response: { status: 503, data: {} } })).toBe(true);
    expect(isTransientApiError({ response: { status: 401, data: {} } })).toBe(false);
  });

  it('never renders a server dump produced by the developer exception page', () => {
    const stackTrace = [
      'System.InvalidOperationException: An exception has been raised that is likely due to a transient failure.',
      ' ---> Npgsql.NpgsqlException: Exception while reading from stream',
      '   at PathwayNavigator.Api.Services.AuthService.LoginAsync(LoginRequestDto request)',
      'HEADERS =======',
      'Accept: application/json, text/plain, */*',
    ].join('\n');

    expect(getApiErrorMessage({ response: { status: 500, data: stackTrace } })).toBe(SERVER_ERROR_MESSAGE);
    expect(getApiErrorMessage({ response: { status: 500, data: '<!DOCTYPE html><html><body>Internal Server Error</body></html>' } })).toBe(SERVER_ERROR_MESSAGE);
  });

  it('caps how much of a message reaches the banner', () => {
    const longMessage = `${'a'.repeat(500)}!`;
    const result = getApiErrorMessage({ response: { status: 400, data: { message: longMessage } } });
    expect(result.length).toBeLessThanOrEqual(300);
    expect(result.endsWith('…')).toBe(true);
  });

  it('uses the problem title for 4xx responses without a dedicated message', () => {
    expect(getApiErrorMessage({ response: { status: 404, data: { title: 'Review record not found.' } } })).toBe('Review record not found.');
  });
});
