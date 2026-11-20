import { useState } from 'react';
import { updateTaskStatus } from '../api/tasksApi.js';

const without = (record, key) => {
  if (!(key in record)) {
    return record;
  }

  const { [key]: _removed, ...rest } = record;
  return rest;
};

/**
 * Per-row status changes through PATCH /api/tasks/{id}/status (last write wins).
 * - While a change is in flight, `pendingById[id]` holds the new status, so the row can show it
 *   optimistically and disable its control; other rows are unaffected.
 * - On success, `onChanged(updatedTask)` receives the task with its new rowVersion.
 * - On failure, the pending value is dropped (the row reverts) and `errorById[id]` explains why,
 *   except for 404: the task is gone, so `onMissing(task)` lets the page refresh instead.
 *
 * @param {{
 *   onChanged: (task: import('../api/tasksApi.js').Task) => void,
 *   onMissing: (task: import('../api/tasksApi.js').Task) => void,
 * }} callbacks
 */
export function useStatusChange({ onChanged, onMissing }) {
  const [pendingById, setPendingById] = useState({});
  const [errorById, setErrorById] = useState({});

  /**
   * @param {import('../api/tasksApi.js').Task} task
   * @param {import('../api/tasksApi.js').TaskStatus} status
   */
  const changeStatus = async (task, status) => {
    if (status === task.status || task.id in pendingById) {
      return;
    }

    setPendingById((previous) => ({ ...previous, [task.id]: status }));
    setErrorById((previous) => without(previous, task.id));

    try {
      const updated = await updateTaskStatus(task.id, status);
      onChanged(updated);
    } catch (error) {
      if (error?.status === 404) {
        onMissing(task);
      } else {
        const reason = [error?.title, error?.detail].filter(Boolean).join(' ');
        setErrorById((previous) => ({
          ...previous,
          [task.id]: `Couldn't change the status of “${task.title}”. ${reason}`.trim(),
        }));
      }
    } finally {
      setPendingById((previous) => without(previous, task.id));
    }
  };

  /** @param {number} id */
  const dismissError = (id) => setErrorById((previous) => without(previous, id));

  return { changeStatus, pendingById, errorById, dismissError };
}
