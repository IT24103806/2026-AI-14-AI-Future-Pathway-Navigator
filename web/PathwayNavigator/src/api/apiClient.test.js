// @vitest-environment jsdom
import { afterEach, describe, expect, it } from 'vitest';
import apiClient from './apiClient';

// A controllable axios adapter: every entry in `responses` describes what the "server" answers.
// A number is a status code; anything else is returned as the response body with status 200.
const stubAdapter = (responses) => {
  const calls = [];
  const adapter = async (config) => {
    calls.push({ method: config.method, url: config.url });
    const next = responses.length > 1 ? responses.shift() : responses[0];
    if (typeof next === 'number') {
      const error = new Error(`Request failed with status code ${next}`);
      error.config = config;
      error.response = { status: next, statusText: 'Error', data: {}, headers: {}, config };
      throw error;
    }
    return { status: 200, statusText: 'OK', data: next, headers: {}, config };
  };
  return { adapter, calls };
};

const originalAdapter = apiClient.defaults.adapter;

afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
  localStorage.clear();
});

describe('apiClient transient-failure retry', () => {
  it('repeats an idempotent GET after a 503 and succeeds', async () => {
    const { adapter, calls } = stubAdapter([503, { ok: true }]);
    apiClient.defaults.adapter = adapter;

    const response = await apiClient.get('/profile/status');

    expect(response.data).toEqual({ ok: true });
    expect(calls).toHaveLength(2);
  });

  it('gives up after the configured number of retries', async () => {
    const { adapter, calls } = stubAdapter([503]);
    apiClient.defaults.adapter = adapter;

    await expect(apiClient.get('/profile/status')).rejects.toBeTruthy();
    expect(calls).toHaveLength(3); // 1 attempt + 2 retries
  });

  it('does not repeat a POST that did not opt in', async () => {
    const { adapter, calls } = stubAdapter([503]);
    apiClient.defaults.adapter = adapter;

    await expect(apiClient.post('/auth/register', { email: 'a@b.c' })).rejects.toBeTruthy();
    expect(calls).toHaveLength(1);
  });

  it('repeats an opted-in POST (sign-in is read-only)', async () => {
    const { adapter, calls } = stubAdapter([503, { token: 'jwt' }]);
    apiClient.defaults.adapter = adapter;

    const response = await apiClient.post('/auth/login', { email: 'a@b.c' }, { retryOnTransient: true });

    expect(response.data).toEqual({ token: 'jwt' });
    expect(calls).toHaveLength(2);
  });

  it('does not retry a normal client error', async () => {
    const { adapter, calls } = stubAdapter([401]);
    apiClient.defaults.adapter = adapter;

    await expect(apiClient.get('/profile/status')).rejects.toBeTruthy();
    expect(calls).toHaveLength(1);
  });
});
