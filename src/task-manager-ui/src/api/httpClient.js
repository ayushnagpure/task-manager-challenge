import { ApiError } from './ApiError.js';
import { getApiBaseUrl } from './config.js';

const GENERIC_TITLES = {
  400: 'The request was not valid.',
  404: 'The requested item was not found.',
  409: 'This item was changed by someone else. Reload it and try again.',
};

/**
 * Joins the API base URL and path, appending only query values that are not empty.
 * @param {string} path Path starting with '/', e.g. '/api/tasks'.
 * @param {Record<string, string | number | null | undefined>} [query]
 * @returns {string}
 */
export function buildUrl(path, query) {
  const url = `${getApiBaseUrl()}${path}`;
  if (!query) {
    return url;
  }

  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && String(value).trim() !== '') {
      params.append(key, String(value));
    }
  }

  const queryString = params.toString();
  return queryString ? `${url}?${queryString}` : url;
}

/**
 * Normalises ValidationProblemDetails `errors` into `{ field: messages[] }` with the same camelCase
 * keys as the JSON body. JSON-path keys from the API's parser ("$.priority") become "priority";
 * errors about the whole body ("$" or "") go under "_form".
 * @param {unknown} errors
 * @returns {Record<string, string[]>}
 */
export function normalizeFieldErrors(errors) {
  if (!errors || typeof errors !== 'object') {
    return {};
  }

  /** @type {Record<string, string[]>} */
  const result = {};
  for (const [key, messages] of Object.entries(errors)) {
    const field = key === '$' || key === '' ? '_form' : key.replace(/^\$\./, '');
    const list = Array.isArray(messages) ? messages.map(String) : [String(messages)];
    result[field] = [...(result[field] ?? []), ...list];
  }

  return result;
}

/**
 * Turns an error response into an ApiError, parsing RFC 7807 ProblemDetails when the body is JSON.
 * @param {Response} response
 * @returns {Promise<ApiError>}
 */
export async function toApiError(response) {
  const contentType = response.headers.get('content-type') ?? '';
  let problem = null;
  if (contentType.includes('json')) {
    try {
      problem = await response.json();
    } catch {
      problem = null;
    }
  }

  const fallbackTitle =
    GENERIC_TITLES[response.status] ??
    (response.status >= 500 ? 'Something went wrong on the server.' : `Request failed (${response.status}).`);

  return new ApiError({
    kind: 'http',
    status: response.status,
    title: typeof problem?.title === 'string' ? problem.title : fallbackTitle,
    detail: typeof problem?.detail === 'string' ? problem.detail : null,
    fieldErrors: normalizeFieldErrors(problem?.errors),
    traceId: typeof problem?.traceId === 'string' ? problem.traceId : null,
  });
}

/**
 * The only place the UI talks to the network.
 * @param {string} path Path starting with '/'.
 * @param {object} [options]
 * @param {'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'} [options.method]
 * @param {Record<string, string | number | null | undefined>} [options.query] Empty values are omitted.
 * @param {unknown} [options.body] Sent as JSON.
 * @param {AbortSignal} [options.signal] Aborting rejects with the original AbortError.
 * @returns {Promise<any>} Parsed JSON, or null for 204 / empty responses.
 * @throws {ApiError} For error statuses, network failures and missing configuration.
 */
export async function request(path, { method = 'GET', query, body, signal } = {}) {
  const url = buildUrl(path, query);

  /** @type {Record<string, string>} */
  const headers = { Accept: 'application/json' };
  /** @type {RequestInit} */
  const init = { method, headers, signal };
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    init.body = JSON.stringify(body);
  }

  let response;
  try {
    response = await fetch(url, init);
  } catch (error) {
    if (error?.name === 'AbortError') {
      throw error; // cancelled on purpose (e.g. filters changed); callers ignore it
    }

    throw new ApiError({
      kind: 'network',
      title: `Can't reach the API at ${getApiBaseUrl()}.`,
      detail: 'Check that the API is running (dotnet run --project src/TaskManager.Api --launch-profile http) and try again.',
    });
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return null;
  }

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}
