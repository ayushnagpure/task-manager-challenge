import './ErrorBanner.css';

/**
 * Shows an ApiError (title + detail) with an optional Retry button.
 * @param {{ error: import('../api/ApiError.js').ApiError | Error, onRetry?: () => void }} props
 */
export default function ErrorBanner({ error, onRetry }) {
  const title = error.title ?? 'Something went wrong.';
  const detail = error.title ? error.detail : null;

  return (
    <div className="error-banner" role="alert">
      <div className="error-banner__content">
        <p className="error-banner__title">{title}</p>
        {detail && <p className="error-banner__detail">{detail}</p>}
        {error.traceId && <p className="error-banner__trace">Reference: {error.traceId}</p>}
      </div>
      {onRetry && (
        <button type="button" className="button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}
