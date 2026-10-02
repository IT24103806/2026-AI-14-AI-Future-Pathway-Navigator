import { describe, expect, it } from 'vitest';
import { getApiErrorMessage } from './apiError';

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

  it('falls back to the caller message', () => {
    expect(getApiErrorMessage({ response: { status: 500, data: {} } }, 'Fallback.')).toBe('Fallback.');
  });
});
