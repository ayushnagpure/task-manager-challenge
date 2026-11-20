/** Statuses in workflow order. `value` is what the API expects; `label` is what users see. */
export const STATUSES = [
  { value: 'Todo', label: 'To do' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'Done', label: 'Done' },
];

/**
 * Priorities from most to least urgent. Each has a text label and an icon so the meaning never
 * depends on colour alone.
 */
export const PRIORITIES = [
  { value: 'Critical', label: 'Critical', icon: '▲▲' },
  { value: 'High', label: 'High', icon: '▲' },
  { value: 'Medium', label: 'Medium', icon: '●' },
  { value: 'Low', label: 'Low', icon: '▼' },
];

/** @param {string} value */
export function findStatus(value) {
  return STATUSES.find((status) => status.value === value) ?? { value, label: value };
}

/** @param {string} value */
export function findPriority(value) {
  return PRIORITIES.find((priority) => priority.value === value) ?? { value, label: value, icon: '' };
}
