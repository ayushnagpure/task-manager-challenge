import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react';
import { PRIORITIES, STATUSES } from '../constants/taskOptions.js';
import { DESCRIPTION_MAX } from '../constants/taskRules.js';
import { TASK_FIELDS, validateTask } from '../validation/taskForm.js';
import FormField from './FormField.jsx';
import './TaskForm.css';

const NO_ERRORS = {};
const NO_MESSAGES = [];

/**
 * The task form shared by create and edit. Presentational: it validates on submit (mirroring the API)
 * and hands valid values to `onSubmit`; the caller talks to the API and passes server errors back.
 *
 * @param {{
 *   initialValues: import('../validation/taskForm.js').TaskFormValues,
 *   submitLabel: string,
 *   saving?: boolean,
 *   submitDisabled?: boolean,
 *   serverErrors?: Record<string, string[]>,
 *   serverErrorToken?: number,
 *   formErrors?: string[],
 *   notice?: import('react').ReactNode,
 *   onSubmit: (values: import('../validation/taskForm.js').TaskFormValues) => void,
 *   onCancel: () => void,
 *   onFieldEdited?: (field: string) => void,
 * }} props `serverErrorToken` changes after each failed save so focus can move to the first invalid field.
 */
export default function TaskForm({
  initialValues,
  submitLabel,
  saving = false,
  submitDisabled = false,
  serverErrors = NO_ERRORS,
  serverErrorToken = 0,
  formErrors = NO_MESSAGES,
  notice = null,
  onSubmit,
  onCancel,
  onFieldEdited,
}) {
  const idPrefix = useId();
  const [values, setValues] = useState(initialValues);
  const [clientErrors, setClientErrors] = useState(NO_ERRORS);
  const fieldRefs = useRef({});
  const serverErrorsRef = useRef(serverErrors);
  useLayoutEffect(() => {
    serverErrorsRef.current = serverErrors;
  });

  // Start typing straight away.
  useEffect(() => {
    fieldRefs.current.title?.focus();
  }, []);

  // After the server rejects a save, move focus to the first field it complained about.
  useEffect(() => {
    if (serverErrorToken === 0) {
      return;
    }

    const first = TASK_FIELDS.find((field) => serverErrorsRef.current[field]?.length);
    if (first) {
      fieldRefs.current[first]?.focus();
    }
  }, [serverErrorToken]);

  const idFor = (field) => `${idPrefix}-${field}`;
  const errorsFor = (field) => clientErrors[field] ?? serverErrors[field] ?? NO_MESSAGES;

  const handleChange = (field) => (event) => {
    const { value } = event.target;
    setValues((previous) => ({ ...previous, [field]: value }));
    setClientErrors((previous) => {
      if (!previous[field]) {
        return previous;
      }

      const { [field]: _cleared, ...rest } = previous;
      return rest;
    });
    onFieldEdited?.(field);
  };

  const bind = (field) => ({
    ref: (element) => {
      fieldRefs.current[field] = element;
    },
    value: values[field],
    onChange: handleChange(field),
  });

  const handleSubmit = (event) => {
    event.preventDefault();
    if (saving || submitDisabled) {
      return;
    }

    const errors = validateTask(values);
    setClientErrors(errors);
    const firstInvalid = TASK_FIELDS.find((field) => errors[field]);
    if (firstInvalid) {
      fieldRefs.current[firstInvalid]?.focus();
      return;
    }

    onSubmit(values);
  };

  return (
    <form className="task-form" noValidate onSubmit={handleSubmit} aria-busy={saving}>
      {notice}

      {formErrors.length > 0 && (
        <div className="form-alert" role="alert">
          <p className="form-alert__title">The task could not be saved.</p>
          <ul className="form-alert__list">
            {formErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </div>
      )}

      <fieldset className="task-form__fields" disabled={saving}>
        <legend className="visually-hidden">Task details</legend>

        <FormField id={idFor('title')} label="Title" required errors={errorsFor('title')}>
          {(controlProps) => (
            <input {...controlProps} {...bind('title')} type="text" className="input" autoComplete="off" />
          )}
        </FormField>

        <FormField
          id={idFor('description')}
          label="Description"
          errors={errorsFor('description')}
          hint={`${values.description.length} / ${DESCRIPTION_MAX} characters`}
        >
          {(controlProps) => <textarea {...controlProps} {...bind('description')} className="input" rows={4} />}
        </FormField>

        <div className="task-form__row">
          <FormField id={idFor('status')} label="Status" required errors={errorsFor('status')}>
            {(controlProps) => (
              <select {...controlProps} {...bind('status')} className="input">
                {STATUSES.map((status) => (
                  <option key={status.value} value={status.value}>
                    {status.label}
                  </option>
                ))}
              </select>
            )}
          </FormField>

          <FormField id={idFor('priority')} label="Priority" required errors={errorsFor('priority')}>
            {(controlProps) => (
              <select {...controlProps} {...bind('priority')} className="input">
                {PRIORITIES.map((priority) => (
                  <option key={priority.value} value={priority.value}>
                    {priority.label}
                  </option>
                ))}
              </select>
            )}
          </FormField>
        </div>

        <FormField id={idFor('assignedTo')} label="Assigned to" errors={errorsFor('assignedTo')}>
          {(controlProps) => (
            <input {...controlProps} {...bind('assignedTo')} type="text" className="input" autoComplete="off" />
          )}
        </FormField>
      </fieldset>

      <div className="task-form__actions">
        <button type="button" className="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={saving || submitDisabled}>
          {saving ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
