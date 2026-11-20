import TaskRow from './TaskRow.jsx';
import './TaskList.css';

/**
 * Task table on wide screens; each row stacks into a card below 640px (CSS only).
 * @param {{ tasks: import('../api/tasksApi.js').Task[], busy: boolean }} props
 */
export default function TaskList({ tasks, busy }) {
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
          </tr>
        </thead>
        <tbody>
          {tasks.map((task) => (
            <TaskRow key={task.id} task={task} />
          ))}
        </tbody>
      </table>
    </div>
  );
}
