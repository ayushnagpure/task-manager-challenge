/**
 * A dismissible, non-error information message (e.g. "That task had already been deleted.").
 * @param {{ message: string, onDismiss: () => void }} props
 */
export default function Notice({ message, onDismiss }) {
  return (
    <div className="notice" role="status">
      <span>{message}</span>
      <button type="button" className="notice__dismiss" aria-label="Dismiss notice" onClick={onDismiss}>
        ×
      </button>
    </div>
  );
}
