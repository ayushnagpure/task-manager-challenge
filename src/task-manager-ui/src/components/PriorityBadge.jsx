import { findPriority } from '../constants/taskOptions.js';
import './Badge.css';

/**
 * Priority pill: colour + icon + text label. The text carries the meaning (the icon is decorative),
 * so priority is never conveyed by colour alone.
 * @param {{ priority: import('../api/tasksApi.js').TaskPriority }} props
 */
export default function PriorityBadge({ priority }) {
  const { value, label, icon } = findPriority(priority);

  return (
    <span className={`badge badge--priority-${value.toLowerCase()}`}>
      {icon && (
        <span className="badge__icon" aria-hidden="true">
          {icon}
        </span>
      )}
      {label}
    </span>
  );
}
