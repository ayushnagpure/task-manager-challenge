/**
 * Label + control + optional hint + errors, wired for assistive technology: the label is associated
 * with the control, and the control's accessible description includes its error (and hint), with
 * aria-invalid set while it has errors.
 *
 * @param {{
 *   id: string,
 *   label: string,
 *   required?: boolean,
 *   hint?: string,
 *   errors?: string[],
 *   children: (controlProps: object) => import('react').ReactNode,
 * }} props `children` renders the control and must spread the props it is given.
 */
export default function FormField({ id, label, required = false, hint, errors = [], children }) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = errors.length > 0 ? `${id}-error` : undefined;
  const describedBy = [errorId, hintId].filter(Boolean).join(' ') || undefined;

  return (
    <div className={`field${errors.length > 0 ? ' field--invalid' : ''}`}>
      <label htmlFor={id} className="field__label">
        {label}
        {required && <span className="field__required"> (required)</span>}
      </label>
      {children({
        id,
        'aria-describedby': describedBy,
        'aria-invalid': errors.length > 0 ? true : undefined,
        'aria-required': required ? true : undefined,
      })}
      {errors.length > 0 && (
        <p id={errorId} className="field__error">
          {errors.join(' ')}
        </p>
      )}
      {hint && (
        <p id={hintId} className="field__hint">
          {hint}
        </p>
      )}
    </div>
  );
}
