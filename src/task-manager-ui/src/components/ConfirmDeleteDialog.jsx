import { useState } from 'react';
import { deleteTask } from '../api/tasksApi.js';
import ErrorBanner from './ErrorBanner.jsx';
import Modal from './Modal.jsx';

/**
 * Confirmation step before a (soft) delete. Cancel has initial focus, the safe default.
 * - success → `onDeleted(task)`;
 * - 404 → the task is already gone, which is what the user wanted → `onAlreadyGone(task)`;
 * - anything else → the dialog stays open with the error so the user can retry or cancel.
 *
 * @param {{
 *   task: import('../api/tasksApi.js').Task,
 *   onCancel: () => void,
 *   onDeleted: (task: import('../api/tasksApi.js').Task) => void,
 *   onAlreadyGone: (task: import('../api/tasksApi.js').Task) => void,
 * }} props
 */
export default function ConfirmDeleteDialog({ task, onCancel, onDeleted, onAlreadyGone }) {
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState(null);

  const confirm = async () => {
    if (deleting) {
      return;
    }

    setDeleting(true);
    setError(null);
    try {
      await deleteTask(task.id);
      onDeleted(task);
    } catch (failure) {
      if (failure?.status === 404) {
        onAlreadyGone(task);
        return;
      }

      setError(failure);
      setDeleting(false);
    }
  };

  return (
    <Modal title="Delete task?" onClose={onCancel} busy={deleting}>
      <p className="confirm-delete__text">
        “<strong>{task.title}</strong>” will be removed from the task list.
      </p>
      {error && <ErrorBanner error={error} />}
      <div className="task-form__actions">
        <button type="button" className="button" onClick={onCancel} disabled={deleting} data-autofocus>
          Cancel
        </button>
        <button type="button" className="button button--danger-solid" onClick={confirm} disabled={deleting}>
          {deleting ? 'Deleting…' : 'Delete task'}
        </button>
      </div>
    </Modal>
  );
}
