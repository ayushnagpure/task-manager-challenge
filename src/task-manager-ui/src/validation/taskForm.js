import { ASSIGNED_TO_MAX, DESCRIPTION_MAX, TITLE_MAX } from '../constants/taskRules.js';
import { PRIORITIES, STATUSES } from '../constants/taskOptions.js';

/** Editable fields, in form order. Keys match the API's camelCase JSON and error keys. */
export const TASK_FIELDS = ['title', 'description', 'status', 'priority', 'assignedTo'];

export const FIELD_LABELS = {
  title: 'Title',
  description: 'Description',
  status: 'Status',
  priority: 'Priority',
  assignedTo: 'Assigned to',
};

/**
 * Form values are always strings (controlled inputs); null fields from the API become ''.
 * @typedef {{ title: string, description: string, status: string, priority: string, assignedTo: string }} TaskFormValues
 */

/** @type {TaskFormValues} Defaults for a new task. */
export const NEW_TASK_VALUES = { title: '', description: '', status: 'Todo', priority: 'Medium', assignedTo: '' };

/**
 * @param {import('../api/tasksApi.js').Task} task
 * @returns {TaskFormValues}
 */
export function valuesFromTask(task) {
  return {
    title: task.title ?? '',
    description: task.description ?? '',
    status: task.status,
    priority: task.priority,
    assignedTo: task.assignedTo ?? '',
  };
}

/**
 * Client-side checks that mirror the API's rules and wording. Lengths are measured on the
 * untrimmed value, as the server does (JS and .NET both count UTF-16 code units).
 * @param {TaskFormValues} values
 * @returns {Record<string, string[]>} Messages per field; empty when valid.
 */
export function validateTask(values) {
  /** @type {Record<string, string[]>} */
  const errors = {};
  const add = (field, message) => {
    (errors[field] ??= []).push(message);
  };

  const title = values.title ?? '';
  if (title.trim() === '') {
    add('title', 'Title is required.');
  } else if (title.length > TITLE_MAX) {
    add('title', `Title must be at most ${TITLE_MAX} characters.`);
  }

  if ((values.description ?? '').length > DESCRIPTION_MAX) {
    add('description', `Description must be at most ${DESCRIPTION_MAX} characters.`);
  }

  if ((values.assignedTo ?? '').length > ASSIGNED_TO_MAX) {
    add('assignedTo', `Assigned to must be at most ${ASSIGNED_TO_MAX} characters.`);
  }

  if (!values.status) {
    add('status', 'Status is required.');
  } else if (!STATUSES.some((s) => s.value === values.status)) {
    add('status', 'Status must be one of: Todo, InProgress, Done.');
  }

  if (!values.priority) {
    add('priority', 'Priority is required.');
  } else if (!PRIORITIES.some((p) => p.value === values.priority)) {
    add('priority', 'Priority must be one of: Low, Medium, High, Critical.');
  }

  return errors;
}

const blankToNull = (value) => {
  const trimmed = (value ?? '').trim();
  return trimmed === '' ? null : trimmed;
};

/**
 * Builds the request body. Always includes every editable field (PUT is a full replacement), so an
 * emptied description or assignee is sent as null and cleared on the server.
 * @param {TaskFormValues} values
 * @param {string} [rowVersion] Include when editing (required by PUT).
 * @returns {import('../api/tasksApi.js').TaskInput}
 */
export function toTaskInput(values, rowVersion) {
  /** @type {import('../api/tasksApi.js').TaskInput} */
  const input = {
    title: values.title.trim(),
    description: blankToNull(values.description),
    status: values.status,
    priority: values.priority,
    assignedTo: blankToNull(values.assignedTo),
  };

  if (rowVersion !== undefined) {
    input.rowVersion = rowVersion;
  }

  return input;
}

/**
 * Fields whose value differs between two sets of form values (compared after trimming).
 * @param {TaskFormValues} before
 * @param {TaskFormValues} after
 * @returns {{ field: string, label: string, value: string }[]} The changed fields with their value in `after`.
 */
export function changedFields(before, after) {
  return TASK_FIELDS.filter((field) => (before[field] ?? '').trim() !== (after[field] ?? '').trim()).map((field) => ({
    field,
    label: FIELD_LABELS[field],
    value: after[field] ?? '',
  }));
}
