import { useState } from 'react';
import EmptyState from './components/EmptyState.jsx';
import ErrorBanner from './components/ErrorBanner.jsx';
import TaskEditor from './components/TaskEditor.jsx';
import TaskFilters from './components/TaskFilters.jsx';
import TaskList from './components/TaskList.jsx';
import { useTasks } from './hooks/useTasks.js';
import './App.css';

const NO_FILTERS = { status: '', priority: '' };

export default function App() {
  const [filters, setFilters] = useState(NO_FILTERS);
  const [editor, setEditor] = useState(null); // { mode: 'create' } | { mode: 'edit', taskId } | null
  const [announcement, setAnnouncement] = useState('');
  const { tasks, loading, loaded, error, reload, replaceTask } = useTasks(filters);
  const hasFilters = Boolean(filters.status || filters.priority);
  const clearFilters = () => setFilters(NO_FILTERS);

  const closeEditor = () => setEditor(null);

  const handleSaved = (task, mode) => {
    if (mode === 'edit') {
      replaceTask(task); // show the saved values (and new rowVersion) right away
    }

    setEditor(null);
    reload(); // re-apply filters and sort order
    setAnnouncement(mode === 'create' ? `Task "${task.title}" created.` : `Task "${task.title}" saved.`);
  };

  const handleTaskMissing = () => {
    setEditor(null);
    reload();
  };

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
            <div className="panel__heading">
              <h2 id="tasks-heading" className="panel__title">
                Tasks
              </h2>
              {loaded && !error && (
                <span className="panel__count" aria-live="polite">
                  {tasks.length} {tasks.length === 1 ? 'task' : 'tasks'}
                </span>
              )}
            </div>
            <button type="button" className="button button--primary" onClick={() => setEditor({ mode: 'create' })}>
              New task
            </button>
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

          {!error && loaded && tasks.length > 0 && (
            <TaskList tasks={tasks} busy={loading} onEdit={(task) => setEditor({ mode: 'edit', taskId: task.id })} />
          )}
        </section>
      </main>

      <div className="visually-hidden" aria-live="polite">
        {announcement}
      </div>

      {editor && (
        <TaskEditor
          key={editor.mode === 'edit' ? `edit-${editor.taskId}` : 'create'}
          mode={editor.mode}
          taskId={editor.taskId}
          onClose={closeEditor}
          onSaved={handleSaved}
          onTaskMissing={handleTaskMissing}
        />
      )}
    </div>
  );
}
