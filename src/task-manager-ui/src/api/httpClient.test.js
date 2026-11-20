import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from './ApiError.js';
import { buildUrl, normalizeFieldErrors, request } from './httpClient.js';

const BASE = 'http://api.test';

/** @param {number} status @param {unknown} [body] @param {string} [contentType] */
function respond(status, body, contentType = 'application/json') {
  return new Response(body === undefined ? null : typeof body === 'string' ? body : JSON.stringify(body), {
    status,
    headers: body === undefined ? {} : { 'Content-Type': contentType },
  });
}

let fetchMock;

beforeEach(() => {
  vi.stubEnv('VITE_API_BASE_URL', `${BASE}/`); // trailing slash is trimmed
  fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe('buildUrl', () => {
  it('joins the base URL and path without a double slash', () => {
    expect(buildUrl('/api/tasks')).toBe('http://api.test/api/tasks');
  });

  it('omits empty, null and undefined query values', () => {
    expect(buildUrl('/api/tasks', { status: '', priority: undefined, extra: null })).toBe('http://api.test/api/tasks');
    expect(buildUrl('/api/tasks', { status: 'Done', priority: '  ' })).toBe('http://api.test/api/tasks?status=Done');
  });

  it('includes and encodes every non-empty value', () => {
    expect(buildUrl('/api/tasks', { status: 'InProgress', priority: 'High' })).toBe(
      'http://api.test/api/tasks?status=InProgress&priority=High',
    );
    expect(buildUrl('/x', { q: 'a&b c' })).toBe('http://api.test/x?q=a%26b+c');
  });

  it('throws a config ApiError when VITE_API_BASE_URL is missing', () => {
    vi.stubEnv('VITE_API_BASE_URL', '');

    expect(() => buildUrl('/api/tasks')).toThrow(ApiError);
    try {
      buildUrl('/api/tasks');
    } catch (error) {
      expect(error.kind).toBe('config');
    }
  });
});

describe('request', () => {
  it('sends GET with an Accept header and returns parsed JSON', async () => {
    fetchMock.mockResolvedValue(respond(200, [{ id: 1 }]));

    const result = await request('/api/tasks', { query: { status: 'Done' } });

    expect(result).toEqual([{ id: 1 }]);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('http://api.test/api/tasks?status=Done');
    expect(init.method).toBe('GET');
    expect(init.headers.Accept).toBe('application/json');
    expect(init.body).toBeUndefined();
  });

  it('sends a JSON body with Content-Type for writes', async () => {
    fetchMock.mockResolvedValue(respond(201, { id: 16 }));

    await request('/api/tasks', { method: 'POST', body: { title: 'New', priority: 'High' } });

    const [, init] = fetchMock.mock.calls[0];
    expect(init.method).toBe('POST');
    expect(init.headers['Content-Type']).toBe('application/json');
    expect(JSON.parse(init.body)).toEqual({ title: 'New', priority: 'High' });
  });

  it('passes the abort signal to fetch', async () => {
    fetchMock.mockResolvedValue(respond(200, []));
    const controller = new AbortController();

    await request('/api/tasks', { signal: controller.signal });

    expect(fetchMock.mock.calls[0][1].signal).toBe(controller.signal);
  });

  it('returns null for 204 No Content', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

    await expect(request('/api/tasks/1', { method: 'DELETE' })).resolves.toBeNull();
  });

  it('parses a 400 ValidationProblemDetails into title and camelCase field errors', async () => {
    fetchMock.mockResolvedValue(
      respond(
        400,
        {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: {
            title: ['Title is required.'],
            priority: ['Priority must be one of: Low, Medium, High, Critical.'],
          },
          traceId: '00-abc-def-00',
        },
        'application/problem+json',
      ),
    );

    const error = await request('/api/tasks', { method: 'POST', body: {} }).catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.kind).toBe('http');
    expect(error.status).toBe(400);
    expect(error.title).toBe('One or more validation errors occurred.');
    expect(error.fieldErrors).toEqual({
      title: ['Title is required.'],
      priority: ['Priority must be one of: Low, Medium, High, Critical.'],
    });
    expect(error.traceId).toBe('00-abc-def-00');
  });

  it('parses a 404 ProblemDetails title and detail', async () => {
    fetchMock.mockResolvedValue(
      respond(
        404,
        { title: 'Resource not found', status: 404, detail: "Task with id '999' was not found.", traceId: 't' },
        'application/problem+json',
      ),
    );

    const error = await request('/api/tasks/999').catch((e) => e);

    expect(error.status).toBe(404);
    expect(error.title).toBe('Resource not found');
    expect(error.detail).toBe("Task with id '999' was not found.");
    expect(error.fieldErrors).toEqual({});
  });

  it('uses a generic title when the error body is not JSON', async () => {
    fetchMock.mockResolvedValue(respond(502, '<html>Bad gateway</html>', 'text/html'));

    const error = await request('/api/tasks').catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(502);
    expect(error.title).toBe('Something went wrong on the server.');
    expect(error.detail).toBeNull();
  });

  it('turns a fetch failure into a network ApiError that names the API URL', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    const error = await request('/api/tasks').catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.kind).toBe('network');
    expect(error.status).toBe(0);
    expect(error.title).toBe("Can't reach the API at http://api.test.");
    expect(error.detail).toMatch(/dotnet run/);
  });

  it('lets an AbortError through unchanged', async () => {
    const abort = new DOMException('The operation was aborted.', 'AbortError');
    fetchMock.mockRejectedValue(abort);

    await expect(request('/api/tasks')).rejects.toBe(abort);
  });
});

describe('normalizeFieldErrors', () => {
  it('maps JSON-path keys to field names and whole-body errors to _form', () => {
    expect(
      normalizeFieldErrors({
        '$.priority': ['The input was not valid.'],
        $: ['The input was not valid.'],
        title: ['Title is required.'],
      }),
    ).toEqual({
      priority: ['The input was not valid.'],
      _form: ['The input was not valid.'],
      title: ['Title is required.'],
    });
  });

  it('returns an empty object when there are no errors', () => {
    expect(normalizeFieldErrors(undefined)).toEqual({});
    expect(normalizeFieldErrors(null)).toEqual({});
  });
});
