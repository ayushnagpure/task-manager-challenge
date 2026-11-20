import { useEffect, useRef, useState } from 'react';
import { createTask, getTask, updateTask } from '../api/tasksApi.js';
import { changedFields, NEW_TASK_VALUES, TASK_FIELDS, toTaskInput, valuesFromTask } from '../validation/taskForm.js';
import ConflictNotice from './ConflictNotice.jsx';
import ErrorBanner from './ErrorBanner.jsx';
import Modal from './Modal.jsx';
import TaskForm from './TaskForm.jsx';
import UnsavedEdits from './UnsavedEdits.jsx';

const KNOWN_FIELDS = new Set(TASK_FIELDS);
const NO_ERRORS = {};

/** Splits a 400's field errors into per-field messages and form-level ones (`_form` and unknown keys). */
function splitServerErrors(error) {
  const fields = {};
  const form = [];
  for (const [key, messages] of Object.entries(error.fieldErrors ?? {})) {
    if (KNOWN_FIELDS.has(key)) {
      fields[key] = messages;
    } else {
      form.push(...messages);
    }
  }

  if (Object.keys(fields).length === 0 && form.length === 0) {
    form.push(describe(error));
  }

  return { fields, form };
}

const describe = (error) => [error?.title ?? 'The task could not be saved.', error?.detail].filter(Boolean).join(' ');

/**
 * Create or edit a task in a modal. Owns the API calls for the form:
 * - edit loads the task fresh (current rowVersion; 404 if it was deleted meanwhile);
 * - 400 field errors go next to their fields, anything else into a form-level alert;
 * - 409 keeps the user's values and offers to load the latest version;
 * - one request at a time (no double submit).
 *
 * @param {{
 *   mode: 'create' | 'edit',
 *   taskId?: number,
 *   onClose: () => void,
 *   onSaved: (task: import('../api/tasksApi.js').Task, mode: 'create' | 'edit') => void,
 *   onTaskMissing: () => void,
 * }} props
 */
export default function TaskEditor({ mode, taskId, onClose, onSaved, onTaskMissing }) {
  const isEdit = mode === 'edit';

  const [phase, setPhase] = useState(isEdit ? 'loading' : 'ready'); // loading | ready | missing | loadError
  const [loadError, setLoadError] = useState(null);
  const [loadAttempt, setLoadAttempt] = useState(0);

  const [initialValues, setInitialValues] = useState(isEdit ? null : NEW_TASK_VALUES);
  const [rowVersion, setRowVersion] = useState(undefined);
  const [formKey, setFormKey] = useState(0);
  const originalValues = useRef(initialValues);

  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);
  const [serverErrors, setServerErrors] = useState(NO_ERRORS);
  const [serverErrorToken, setServerErrorToken] = useState(0);
  const [formErrors, setFormErrors] = useState([]);

  const [conflict, setConflict] = useState(null); // { attempted: TaskFormValues }
  const [loadingLatest, setLoadingLatest] = useState(false);
  const [unsavedEdits, setUnsavedEdits] = useState(null);

  /** Shows `task` in a fresh form and remembers it as the version being edited. */
  const showTask = (task) => {
    const values = valuesFromTask(task);
    originalValues.current = values;
    setInitialValues(values);
    setRowVersion(task.rowVersion);
    setFormKey((key) => key + 1);
  };

  // Edit: always load the latest copy rather than trusting the (possibly stale) list row.
  useEffect(() => {
    if (!isEdit) {
      return undefined;
    }

    const controller = new AbortController();
    setPhase('loading');
    getTask(taskId, { signal: controller.signal })
      .then((task) => {
        if (!controller.signal.aborted) {
          showTask(task);
          setPhase('ready');
        }
      })
      .catch((error) => {
        if (controller.signal.aborted || error?.name === 'AbortError') {
          return;
        }

        if (error?.status === 404) {
          setPhase('missing');
        } else {
          setLoadError(error);
          setPhase('loadError');
        }
      });

    return () => controller.abort();
    // showTask is not a dependency: it only uses state setters and a ref.
  }, [isEdit, taskId, loadAttempt]);

  const save = async (values) => {
    if (savingRef.current) {
      return; // a request is already in flight
    }

    savingRef.current = true;
    setSaving(true);
    setServerErrors(NO_ERRORS);
    setFormErrors([]);

    try {
      const saved = isEdit
        ? await updateTask(taskId, toTaskInput(values, rowVersion))
        : await createTask(toTaskInput(values));
      onSaved(saved, mode);
    } catch (error) {
      if (error?.status === 409) {
        setConflict({ attempted: values });
        setUnsavedEdits(null);
      } else if (error?.status === 404 && isEdit) {
        setPhase('missing');
      } else if (error?.status === 400) {
        const { fields, form } = splitServerErrors(error);
        setServerErrors(fields);
        setFormErrors(form);
        setServerErrorToken((token) => token + 1);
      } else {
        setFormErrors([describe(error)]);
      }
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  };

  const loadLatest = async () => {
    setLoadingLatest(true);
    setFormErrors([]);
    try {
      const latest = await getTask(taskId);
      setUnsavedEdits(changedFields(originalValues.current, conflict.attempted));
      setConflict(null);
      setServerErrors(NO_ERRORS);
      showTask(latest);
    } catch (error) {
      if (error?.status === 404) {
        setPhase('missing');
      } else {
        setFormErrors([describe(error)]);
      }
    } finally {
      setLoadingLatest(false);
    }
  };

  const clearServerError = (field) => {
    setServerErrors((previous) => {
      if (!previous[field]) {
        return previous;
      }

      const { [field]: _cleared, ...rest } = previous;
      return rest;
    });
  };

  let body;
  if (phase === 'loading') {
    body = (
      <div className="editor-state">
        <p className="editor-state__message" role="status">
          Loading task…
        </p>
        <div className="task-form__actions">
          <button type="button" className="button" onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    );
  } else if (phase === 'missing') {
    body = (
      <div className="editor-state">
        <p className="editor-state__message" role="alert">
          This task no longer exists. It may have been deleted.
        </p>
        <div className="task-form__actions">
          <button type="button" className="button button--primary" onClick={onTaskMissing}>
            Close
          </button>
        </div>
      </div>
    );
  } else if (phase === 'loadError') {
    body = (
      <div className="editor-state">
        <ErrorBanner error={loadError} onRetry={() => setLoadAttempt((attempt) => attempt + 1)} />
        <div className="task-form__actions">
          <button type="button" className="button" onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    );
  } else {
    body = (
      <TaskForm
        key={formKey}
        initialValues={initialValues}
        submitLabel={isEdit ? 'Save changes' : 'Create task'}
        saving={saving}
        submitDisabled={Boolean(conflict) || loadingLatest}
        serverErrors={serverErrors}
        serverErrorToken={serverErrorToken}
        formErrors={formErrors}
        onSubmit={save}
        onCancel={onClose}
        onFieldEdited={clearServerError}
        notice={
          <>
            {conflict && <ConflictNotice onLoadLatest={loadLatest} onDiscard={onClose} loading={loadingLatest} />}
            {unsavedEdits && <UnsavedEdits edits={unsavedEdits} />}
          </>
        }
      />
    );
  }

  return (
    <Modal title={isEdit ? 'Edit task' : 'New task'} onClose={phase === 'missing' ? onTaskMissing : onClose} busy={saving}>
      {body}
    </Modal>
  );
}
