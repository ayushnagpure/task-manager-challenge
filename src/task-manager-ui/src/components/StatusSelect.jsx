import { useId } from 'react';
import { STATUSES } from '../constants/taskOptions.js';

/**
 * Per-row status dropdown. Shows the pending status optimistically while a change is saving (and is
 * disabled meanwhile); if the change fails, the parent drops the pending value so this reverts, and
 * the error is shown right under the control.
 *
 * @param {{
 *   task: import('../api/tasksApi.js').Task,
 *   pendingStatus?: string,
 *   error?: string,
 *   onChange: (task: import('../api/tasksApi.js').Task, status: string) => void,
 *   onDismissError: (id: number) => void,
 * }} props
 */
export default function StatusSelect({ task, pendingStatus, error, onChange, onDismissError }) {
  const errorId = useId();
  const saving = pendingStatus !== undefined;
  const value = pendingStatus ?? task.status;

  return (
    <div className="status-select">
      <div className="status-select__control">
        <select
          className={`status-select__select status-select__select--${value.toLowerCase()}`}
          aria-label={`Status for ${task.title}`}
          aria-describedby={error ? errorId : undefined}
          value={value}
          disabled={saving}
          onChange={(event) => onChange(task, event.target.value)}
        >
          {STATUSES.map((status) => (
            <option key={status.value} value={status.value}>
              {status.label}
            </option>
          ))}
        </select>
        {/* Always rendered so screen readers pick up the change. */}
        <span className="status-select__saving" aria-live="polite">
          {saving ? 'Saving…' : ''}
        </span>
      </div>
      {error && (
        <div id={errorId} className="status-select__error" role="alert">
          <span>{error}</span>
          <button
            type="button"
            className="status-select__dismiss"
            aria-label={`Dismiss error for ${task.title}`}
            onClick={() => onDismissError(task.id)}
          >
            ×
          </button>
        </div>
      )}
    </div>
  );
}
