/**
 * The single error type thrown by the API layer. Components only ever deal with this shape.
 *
 * - `kind: 'http'`    — the API answered with an error status (parsed from ProblemDetails when possible).
 * - `kind: 'network'` — the request never got an answer (API not running, CORS blocked, offline).
 * - `kind: 'config'`  — the UI is misconfigured (VITE_API_BASE_URL missing).
 */
export class ApiError extends Error {
  /**
   * @param {object} init
   * @param {'http' | 'network' | 'config'} [init.kind]
   * @param {number} [init.status] HTTP status, or 0 when there was no response.
   * @param {string} init.title Short, user-facing summary.
   * @param {string | null} [init.detail] Longer explanation, if any.
   * @param {Record<string, string[]>} [init.fieldErrors] Validation messages keyed by camelCase field name.
   * @param {string | null} [init.traceId] Server trace id, useful when reporting a problem.
   */
  constructor({ kind = 'http', status = 0, title, detail = null, fieldErrors = {}, traceId = null }) {
    super(detail ? `${title} ${detail}` : title);
    this.name = 'ApiError';
    this.kind = kind;
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.fieldErrors = fieldErrors;
    this.traceId = traceId;
  }
}
