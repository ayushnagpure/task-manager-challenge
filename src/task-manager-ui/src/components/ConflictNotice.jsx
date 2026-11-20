import { useEffect, useRef } from 'react';

/**
 * Shown after a 409: someone else saved the task first. The user's values stay in the form; loading
 * the latest version shows the other person's changes and lists the user's edits so they can re-apply them.
 * @param {{ onLoadLatest: () => void, onDiscard: () => void, loading?: boolean }} props
 */
export default function ConflictNotice({ onLoadLatest, onDiscard, loading = false }) {
  const loadButtonRef = useRef(null);

  // Keyboard users land on the next useful action rather than on a disabled Save button.
  useEffect(() => {
    loadButtonRef.current?.focus();
  }, []);

  return (
    <div className="conflict-notice" role="alert">
      <p className="conflict-notice__title">Someone else changed this task after you opened it.</p>
      <p className="conflict-notice__text">
        Your changes have not been saved and are still in the form below. Load the latest version to see what
        changed; your edits will be listed so you can re-apply them.
      </p>
      <div className="conflict-notice__actions">
        <button ref={loadButtonRef} type="button" className="button button--primary" onClick={onLoadLatest} disabled={loading}>
          {loading ? 'Loading…' : 'Load latest version'}
        </button>
        <button type="button" className="button" onClick={onDiscard} disabled={loading}>
          Close without saving
        </button>
      </div>
    </div>
  );
}
