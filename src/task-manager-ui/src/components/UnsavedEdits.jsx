import { findPriority, findStatus } from '../constants/taskOptions.js';

/** @param {{ field: string, value: string }} edit */
function displayValue({ field, value }) {
  if (value.trim() === '') {
    return <em>(empty)</em>;
  }

  if (field === 'status') {
    return findStatus(value).label;
  }

  if (field === 'priority') {
    return findPriority(value).label;
  }

  return value;
}

/**
 * After "Load latest version": the edits the user had made before the conflict, so nothing is lost.
 * @param {{ edits: { field: string, label: string, value: string }[] }} props
 */
export default function UnsavedEdits({ edits }) {
  return (
    <details className="unsaved-edits" open>
      <summary className="unsaved-edits__summary">Your unsaved edits</summary>
      <p className="unsaved-edits__intro">
        The form now shows the latest saved version. Re-apply anything you still want, then save.
      </p>
      {edits.length === 0 ? (
        <p className="unsaved-edits__intro">You had not changed any fields.</p>
      ) : (
        <dl className="unsaved-edits__list">
          {edits.map((edit) => (
            <div key={edit.field} className="unsaved-edits__item">
              <dt>{edit.label}</dt>
              <dd>{displayValue(edit)}</dd>
            </div>
          ))}
        </dl>
      )}
    </details>
  );
}
