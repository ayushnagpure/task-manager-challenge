import { findStatus } from '../constants/taskOptions.js';
import './Badge.css';

/**
 * Status pill with its text label.
 * @param {{ status: import('../api/tasksApi.js').TaskStatus }} props
 */
export default function StatusBadge({ status }) {
  const { value, label } = findStatus(status);

  return (
    <span className={`badge badge--status-${value.toLowerCase()}`}>
      <span className="badge__dot" aria-hidden="true" />
      {label}
    </span>
  );
}
