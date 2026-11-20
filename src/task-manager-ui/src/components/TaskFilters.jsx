import { PRIORITIES, STATUSES } from '../constants/taskOptions.js';
import './TaskFilters.css';

/**
 * Status and priority filters. Purely presentational: reports changes, never calls the API.
 * @param {{
 *   value: { status: string, priority: string },
 *   onChange: (filters: { status: string, priority: string }) => void,
 *   onClear: () => void,
 * }} props
 */
export default function TaskFilters({ value, onChange, onClear }) {
  const hasFilters = Boolean(value.status || value.priority);

  return (
    <div className="filters" role="group" aria-label="Filter tasks">
      <div className="filters__field">
        <label htmlFor="filter-status" className="filters__label">
          Status
        </label>
        <select
          id="filter-status"
          className="filters__select"
          value={value.status}
          onChange={(event) => onChange({ ...value, status: event.target.value })}
        >
          <option value="">All statuses</option>
          {STATUSES.map((status) => (
            <option key={status.value} value={status.value}>
              {status.label}
            </option>
          ))}
        </select>
      </div>

      <div className="filters__field">
        <label htmlFor="filter-priority" className="filters__label">
          Priority
        </label>
        <select
          id="filter-priority"
          className="filters__select"
          value={value.priority}
          onChange={(event) => onChange({ ...value, priority: event.target.value })}
        >
          <option value="">All priorities</option>
          {PRIORITIES.map((priority) => (
            <option key={priority.value} value={priority.value}>
              {priority.label}
            </option>
          ))}
        </select>
      </div>

      <button type="button" className="button filters__clear" onClick={onClear} disabled={!hasFilters}>
        Clear filters
      </button>
    </div>
  );
}
