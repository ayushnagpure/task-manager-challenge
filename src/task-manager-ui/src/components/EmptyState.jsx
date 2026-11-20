import './EmptyState.css';

/**
 * Shown when the list is empty: either nothing matches the filters, or there are no tasks at all.
 * @param {{ filtered: boolean, onClearFilters: () => void }} props
 */
export default function EmptyState({ filtered, onClearFilters }) {
  return (
    <div className="empty-state" role="status">
      <p className="empty-state__title">{filtered ? 'No tasks match these filters' : 'No tasks yet'}</p>
      <p className="empty-state__text">
        {filtered ? 'Try a different status or priority.' : 'Tasks you create will appear here.'}
      </p>
      {filtered && (
        <button type="button" className="button" onClick={onClearFilters}>
          Clear filters
        </button>
      )}
    </div>
  );
}
