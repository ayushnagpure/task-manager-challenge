import { PRIORITIES, STATUSES } from '../constants/taskOptions.js';
import ErrorBanner from './ErrorBanner.jsx';
import PriorityBadge from './PriorityBadge.jsx';
import StatusBadge from './StatusBadge.jsx';
import './TaskSummary.css';

/**
 * Builds a full status x priority grid from the API's sparse list (it omits empty combinations).
 * @param {import('../api/tasksApi.js').TaskSummaryItem[]} items
 */
function toMatrix(items) {
  const counts = Object.fromEntries(STATUSES.map((s) => [s.value, Object.fromEntries(PRIORITIES.map((p) => [p.value, 0]))]));
  for (const { status, priority, count } of items) {
    if (counts[status] && priority in counts[status]) {
      counts[status][priority] += count;
    }
  }

  const rowTotal = (status) => PRIORITIES.reduce((sum, p) => sum + counts[status][p.value], 0);
  const columnTotal = (priority) => STATUSES.reduce((sum, s) => sum + counts[s.value][priority], 0);
  const grandTotal = STATUSES.reduce((sum, s) => sum + rowTotal(s.value), 0);

  return { counts, rowTotal, columnTotal, grandTotal };
}

const Count = ({ value, className = '' }) => (
  <td className={`summary-table__count${value === 0 ? ' summary-table__count--zero' : ''} ${className}`.trim()}>{value}</td>
);

/**
 * Counts of active tasks by status and priority (GET /api/tasks/summary), as a compact matrix with totals.
 * Independent of the list filters.
 *
 * @param {{
 *   items: import('../api/tasksApi.js').TaskSummaryItem[],
 *   loading: boolean,
 *   loaded: boolean,
 *   error: import('../api/ApiError.js').ApiError | null,
 *   onRetry: () => void,
 * }} props
 */
export default function TaskSummary({ items, loading, loaded, error, onRetry }) {
  let content;
  if (error) {
    content = <ErrorBanner error={error} onRetry={onRetry} />;
  } else if (!loaded) {
    content = (
      <p className="summary__message" role="status">
        Loading summary…
      </p>
    );
  } else if (items.length === 0) {
    content = <p className="summary__message">No active tasks yet.</p>;
  } else {
    const { counts, rowTotal, columnTotal, grandTotal } = toMatrix(items);
    content = (
      <div className={`summary-table__wrap${loading ? ' summary-table__wrap--busy' : ''}`} aria-busy={loading}>
        <table className="summary-table">
          <caption className="summary-table__caption">
            Active tasks by status and priority. Not affected by the list filters.
          </caption>
          <thead>
            <tr>
              <th scope="col">
                <span className="visually-hidden">Status</span>
              </th>
              {PRIORITIES.map((priority) => (
                <th key={priority.value} scope="col">
                  <PriorityBadge priority={priority.value} />
                </th>
              ))}
              <th scope="col">Total</th>
            </tr>
          </thead>
          <tbody>
            {STATUSES.map((status) => (
              <tr key={status.value}>
                <th scope="row">
                  <StatusBadge status={status.value} />
                </th>
                {PRIORITIES.map((priority) => (
                  <Count key={priority.value} value={counts[status.value][priority.value]} />
                ))}
                <Count value={rowTotal(status.value)} className="summary-table__total" />
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th scope="row">Total</th>
              {PRIORITIES.map((priority) => (
                <Count key={priority.value} value={columnTotal(priority.value)} className="summary-table__total" />
              ))}
              <Count value={grandTotal} className="summary-table__total summary-table__grand" />
            </tr>
          </tfoot>
        </table>
      </div>
    );
  }

  return (
    <section className="panel summary" aria-labelledby="summary-heading">
      <h2 id="summary-heading" className="panel__title summary__title">
        Summary
      </h2>
      {content}
    </section>
  );
}
