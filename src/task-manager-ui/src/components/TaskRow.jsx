import { formatDateTime } from '../utils/formatDate.js';
import PriorityBadge from './PriorityBadge.jsx';
import StatusBadge from './StatusBadge.jsx';

/**
 * One task in the list. `data-label` drives the stacked card layout on small screens.
 * @param {{ task: import('../api/tasksApi.js').Task, onEdit: (task: import('../api/tasksApi.js').Task) => void }} props
 */
export default function TaskRow({ task, onEdit }) {
  return (
    <tr className="task-row">
      <td className="task-row__main" data-label="Task">
        <span className="task-row__title">{task.title}</span>
        {task.description && <span className="task-row__description">{task.description}</span>}
      </td>
      <td data-label="Status">
        <StatusBadge status={task.status} />
      </td>
      <td data-label="Priority">
        <PriorityBadge priority={task.priority} />
      </td>
      <td data-label="Assigned to">
        {task.assignedTo ? (
          <span>{task.assignedTo}</span>
        ) : (
          <span className="task-row__muted">Unassigned</span>
        )}
      </td>
      <td data-label="Last updated">
        <time className="task-row__date" dateTime={task.modifiedDate}>
          {formatDateTime(task.modifiedDate)}
        </time>
      </td>
      <td className="task-row__actions" data-label="Actions">
        <button type="button" className="button button--small" onClick={() => onEdit(task)} aria-label={`Edit ${task.title}`}>
          Edit
        </button>
      </td>
    </tr>
  );
}
