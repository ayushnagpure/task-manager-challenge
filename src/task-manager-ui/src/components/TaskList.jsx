import TaskRow from './TaskRow.jsx';
import './TaskList.css';

/**
 * Task table on wide screens; each row stacks into a card below 640px (CSS only).
 * @param {{
 *   tasks: import('../api/tasksApi.js').Task[],
 *   busy: boolean,
 *   pendingStatusById?: Record<number, string>,
 *   statusErrorById?: Record<number, string>,
 *   onStatusChange: (task: import('../api/tasksApi.js').Task, status: string) => void,
 *   onDismissStatusError: (id: number) => void,
 *   onEdit: (task: import('../api/tasksApi.js').Task) => void,
 *   onDelete: (task: import('../api/tasksApi.js').Task) => void,
 * }} props
 */
export default function TaskList({
  tasks,
  busy,
  pendingStatusById = {},
  statusErrorById = {},
  onStatusChange,
  onDismissStatusError,
  onEdit,
  onDelete,
}) {
  return (
    <div className={`task-list${busy ? ' task-list--busy' : ''}`} aria-busy={busy}>
      <table className="task-table">
        <caption className="visually-hidden">Tasks, most urgent first</caption>
        <thead>
          <tr>
            <th scope="col">Task</th>
            <th scope="col">Status</th>
            <th scope="col">Priority</th>
            <th scope="col">Assigned to</th>
            <th scope="col">Last updated</th>
            <th scope="col">
              <span className="visually-hidden">Actions</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {tasks.map((task) => (
            <TaskRow
              key={task.id}
              task={task}
              pendingStatus={pendingStatusById[task.id]}
              statusError={statusErrorById[task.id]}
              onStatusChange={onStatusChange}
              onDismissStatusError={onDismissStatusError}
              onEdit={onEdit}
              onDelete={onDelete}
            />
          ))}
        </tbody>
      </table>
    </div>
  );
}
