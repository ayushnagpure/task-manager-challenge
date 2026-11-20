import { useState } from 'react';
import EmptyState from './components/EmptyState.jsx';
import ErrorBanner from './components/ErrorBanner.jsx';
import TaskFilters from './components/TaskFilters.jsx';
import TaskList from './components/TaskList.jsx';
import { useTasks } from './hooks/useTasks.js';
import './App.css';

const NO_FILTERS = { status: '', priority: '' };

export default function App() {
  const [filters, setFilters] = useState(NO_FILTERS);
  const { tasks, loading, loaded, error, reload } = useTasks(filters);
  const hasFilters = Boolean(filters.status || filters.priority);
  const clearFilters = () => setFilters(NO_FILTERS);

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header__inner">
          <h1 className="app-header__title">Task Manager</h1>
          <p className="app-header__subtitle">Track work by status and priority</p>
        </div>
      </header>

      <main className="app-main">
        <section className="panel" aria-labelledby="tasks-heading">
          <div className="panel__header">
            <h2 id="tasks-heading" className="panel__title">
              Tasks
            </h2>
            {loaded && !error && (
              <span className="panel__count" aria-live="polite">
                {tasks.length} {tasks.length === 1 ? 'task' : 'tasks'}
              </span>
            )}
          </div>

          <TaskFilters value={filters} onChange={setFilters} onClear={clearFilters} />

          {error && <ErrorBanner error={error} onRetry={reload} />}

          {!error && !loaded && (
            <p className="panel__loading" role="status">
              Loading tasks…
            </p>
          )}

          {!error && loaded && tasks.length === 0 && (
            <EmptyState filtered={hasFilters} onClearFilters={clearFilters} />
          )}

          {!error && loaded && tasks.length > 0 && <TaskList tasks={tasks} busy={loading} />}
        </section>
      </main>
    </div>
  );
}
