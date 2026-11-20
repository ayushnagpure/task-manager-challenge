import { useEffect, useRef, useState } from 'react';
import ConfirmDeleteDialog from './components/ConfirmDeleteDialog.jsx';
import EmptyState from './components/EmptyState.jsx';
import ErrorBanner from './components/ErrorBanner.jsx';
import Notice from './components/Notice.jsx';
import TaskEditor from './components/TaskEditor.jsx';
import TaskFilters from './components/TaskFilters.jsx';
import TaskList from './components/TaskList.jsx';
import TaskSummary from './components/TaskSummary.jsx';
import { findStatus } from './constants/taskOptions.js';
import { useStatusChange } from './hooks/useStatusChange.js';
import { useSummary } from './hooks/useSummary.js';
import { useTasks } from './hooks/useTasks.js';
import './App.css';

const NO_FILTERS = { status: '', priority: '' };

export default function App() {
  const [filters, setFilters] = useState(NO_FILTERS);
  const [editor, setEditor] = useState(null); // { mode: 'create' } | { mode: 'edit', taskId } | null
  const [taskToDelete, setTaskToDelete] = useState(null);
  const [notice, setNotice] = useState(null);
  const [announcement, setAnnouncement] = useState('');
  const { tasks, loading, loaded, error, reload, replaceTask, removeTask } = useTasks(filters);
  const summary = useSummary();
  const hasFilters = Boolean(filters.status || filters.priority);
  const clearFilters = () => setFilters(NO_FILTERS);

  // After a delete the button that opened the dialog is gone, so focus moves to the list heading.
  const headingRef = useRef(null);
  const [focusHeadingToken, setFocusHeadingToken] = useState(0);
  useEffect(() => {
    if (focusHeadingToken > 0) {
      headingRef.current?.focus();
    }
  }, [focusHeadingToken]);

  /** Re-fetch everything that depends on task data: the (filtered) list and the summary counts. */
  const refreshAll = () => {
    reload();
    summary.reload();
  };

  const statusChange = useStatusChange({
    onChanged: (task) => {
      replaceTask(task); // new status and rowVersion, so a later edit won't get a false 409
      refreshAll(); // re-applies a status filter the task may no longer match
      setAnnouncement(`Status of "${task.title}" changed to ${findStatus(task.status).label}.`);
    },
    onMissing: (task) => {
      removeTask(task.id);
      refreshAll();
      setNotice(`“${task.title}” no longer exists. It may have been deleted.`);
    },
  });

  const handleSaved = (task, mode) => {
    if (mode === 'edit') {
      replaceTask(task); // show the saved values (and new rowVersion) right away
    }

    setEditor(null);
    refreshAll(); // re-apply filters and sort order
    setAnnouncement(mode === 'create' ? `Task "${task.title}" created.` : `Task "${task.title}" saved.`);
  };

  const handleTaskMissing = () => {
    setEditor(null);
    refreshAll();
  };

  const finishDelete = (task, message) => {
    removeTask(task.id);
    setTaskToDelete(null);
    refreshAll();
    setFocusHeadingToken((token) => token + 1);
    return message;
  };

  const handleDeleted = (task) => setAnnouncement(finishDelete(task, `Task "${task.title}" deleted.`));
  const handleAlreadyGone = (task) => setNotice(finishDelete(task, `“${task.title}” had already been deleted.`));

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header__inner">
          <h1 className="app-header__title">Task Manager</h1>
          <p className="app-header__subtitle">Track work by status and priority</p>
        </div>
      </header>

      <main className="app-main">
        <TaskSummary
          items={summary.items}
          loading={summary.loading}
          loaded={summary.loaded}
          error={summary.error}
          onRetry={summary.reload}
        />

        <section className="panel" aria-labelledby="tasks-heading">
          <div className="panel__header">
            <div className="panel__heading">
              <h2 id="tasks-heading" className="panel__title" ref={headingRef} tabIndex={-1}>
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

          {notice && <Notice message={notice} onDismiss={() => setNotice(null)} />}

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
            <TaskList
              tasks={tasks}
              busy={loading}
              pendingStatusById={statusChange.pendingById}
              statusErrorById={statusChange.errorById}
              onStatusChange={statusChange.changeStatus}
              onDismissStatusError={statusChange.dismissError}
              onEdit={(task) => setEditor({ mode: 'edit', taskId: task.id })}
              onDelete={setTaskToDelete}
            />
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
          onClose={() => setEditor(null)}
          onSaved={handleSaved}
          onTaskMissing={handleTaskMissing}
        />
      )}

      {taskToDelete && (
        <ConfirmDeleteDialog
          task={taskToDelete}
          onCancel={() => setTaskToDelete(null)}
          onDeleted={handleDeleted}
          onAlreadyGone={handleAlreadyGone}
        />
      )}
    </div>
  );
}
