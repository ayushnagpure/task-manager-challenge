import { ApiError } from './ApiError.js';

/**
 * Base URL of the API from VITE_API_BASE_URL (see .env.development), without a trailing slash.
 * Read on every call so tests can change it with vi.stubEnv.
 * @returns {string}
 */
export function getApiBaseUrl() {
  const raw = import.meta.env.VITE_API_BASE_URL;
  if (typeof raw !== 'string' || raw.trim() === '') {
    throw new ApiError({
      kind: 'config',
      title: 'The API address is not configured.',
      detail: 'Set VITE_API_BASE_URL (see .env.development) and restart the dev server.',
    });
  }

  return raw.trim().replace(/\/+$/, '');
}
