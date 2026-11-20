import { formatDateTime } from '../utils/formatDate.js';
import PriorityBadge from './PriorityBadge.jsx';
import StatusSelect from './StatusSelect.jsx';

/**
 * One task in the list. `data-label` drives the stacked card layout on small screens.
 * @param {{
 *   task: import('../api/tasksApi.js').Task,
 *   pendingStatus?: string,
 *   statusError?: string,
 *   onStatusChange: (task: import('../api/tasksApi.js').Task, status: string) => void,
 *   onDismissStatusError: (id: number) => void,
 *   onEdit: (task: import('../api/tasksApi.js').Task) => void,
 *   onDelete: (task: import('../api/tasksApi.js').Task) => void,
 * }} props
 */
export default function TaskRow({ task, pendingStatus, statusError, onStatusChange, onDismissStatusError, onEdit, onDelete }) {
  return (
    <tr className="task-row">
      <td className="task-row__main" data-label="Task">
        <span className="task-row__title">{task.title}</span>
        {task.description && <span className="task-row__description">{task.description}</span>}
      </td>
      <td data-label="Status">
        <StatusSelect
          task={task}
          pendingStatus={pendingStatus}
          error={statusError}
          onChange={onStatusChange}
          onDismissError={onDismissStatusError}
        />
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
        <button
          type="button"
          className="button button--small button--danger"
          onClick={() => onDelete(task)}
          aria-label={`Delete ${task.title}`}
        >
          Delete
        </button>
      </td>
    </tr>
  );
}
