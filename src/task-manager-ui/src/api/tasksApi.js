import { request } from './httpClient.js';

/** @typedef {'Todo' | 'InProgress' | 'Done'} TaskStatus */
/** @typedef {'Low' | 'Medium' | 'High' | 'Critical'} TaskPriority */

/**
 * A task as returned by the API.
 * @typedef {object} Task
 * @property {number} id
 * @property {string} title
 * @property {string | null} description
 * @property {TaskStatus} status
 * @property {TaskPriority} priority
 * @property {string | null} assignedTo
 * @property {string} createdDate ISO 8601, UTC.
 * @property {string} modifiedDate ISO 8601, UTC.
 * @property {string} rowVersion Base64 concurrency token; send it back unchanged on update.
 */

/**
 * @typedef {object} TaskFilters
 * @property {TaskStatus | ''} [status] Empty means "all".
 * @property {TaskPriority | ''} [priority] Empty means "all".
 */

/**
 * Fields sent when creating or updating a task.
 * @typedef {object} TaskInput
 * @property {string} title
 * @property {string | null} [description]
 * @property {TaskStatus} [status]
 * @property {TaskPriority} priority
 * @property {string | null} [assignedTo]
 * @property {string} [rowVersion] Required for update.
 */

/**
 * @typedef {object} TaskSummaryItem
 * @property {TaskStatus} status
 * @property {TaskPriority} priority
 * @property {number} count
 */

/**
 * GET /api/tasks?status=&priority= (empty filters are left out of the URL).
 * @param {TaskFilters} [filters]
 * @param {{ signal?: AbortSignal }} [options]
 * @returns {Promise<Task[]>}
 */
export function listTasks(filters = {}, { signal } = {}) {
  return request('/api/tasks', { query: { status: filters.status, priority: filters.priority }, signal });
}

/**
 * GET /api/tasks/{id}
 * @param {number} id
 * @param {{ signal?: AbortSignal }} [options]
 * @returns {Promise<Task>}
 */
export function getTask(id, { signal } = {}) {
  return request(`/api/tasks/${encodeURIComponent(id)}`, { signal });
}

/**
 * POST /api/tasks
 * @param {TaskInput} task
 * @returns {Promise<Task>}
 */
export function createTask(task) {
  return request('/api/tasks', { method: 'POST', body: task });
}

/**
 * PUT /api/tasks/{id} — full update; `task.rowVersion` must be the latest one (409 otherwise).
 * @param {number} id
 * @param {TaskInput} task
 * @returns {Promise<Task>}
 */
export function updateTask(id, task) {
  return request(`/api/tasks/${encodeURIComponent(id)}`, { method: 'PUT', body: task });
}

/**
 * PATCH /api/tasks/{id}/status — last write wins; the response carries the new rowVersion.
 * @param {number} id
 * @param {TaskStatus} status
 * @returns {Promise<Task>}
 */
export function updateTaskStatus(id, status) {
  return request(`/api/tasks/${encodeURIComponent(id)}/status`, { method: 'PATCH', body: { status } });
}

/**
 * DELETE /api/tasks/{id} — soft delete.
 * @param {number} id
 * @returns {Promise<null>}
 */
export function deleteTask(id) {
  return request(`/api/tasks/${encodeURIComponent(id)}`, { method: 'DELETE' });
}

/**
 * GET /api/tasks/summary
 * @param {{ signal?: AbortSignal }} [options]
 * @returns {Promise<TaskSummaryItem[]>}
 */
export function getSummary({ signal } = {}) {
  return request('/api/tasks/summary', { signal });
}
