import { useCallback, useEffect, useState } from 'react';
import { getSummary } from '../api/tasksApi.js';

/**
 * Loads the task summary (counts by status and priority). Call `reload` after anything that changes
 * tasks; a newer request aborts the previous one. Previous counts stay visible while reloading.
 *
 * @returns {{
 *   items: import('../api/tasksApi.js').TaskSummaryItem[],
 *   loading: boolean,
 *   loaded: boolean,
 *   error: import('../api/ApiError.js').ApiError | null,
 *   reload: () => void,
 * }}
 */
export function useSummary() {
  const [state, setState] = useState({ items: [], loading: true, loaded: false, error: null });
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setState((previous) => ({ ...previous, loading: true, error: null }));

    getSummary({ signal: controller.signal })
      .then((items) => {
        if (!controller.signal.aborted) {
          setState({ items, loading: false, loaded: true, error: null });
        }
      })
      .catch((error) => {
        if (controller.signal.aborted || error?.name === 'AbortError') {
          return;
        }

        setState((previous) => ({ ...previous, loading: false, error }));
      });

    return () => controller.abort();
  }, [reloadToken]);

  const reload = useCallback(() => setReloadToken((token) => token + 1), []);

  return { ...state, reload };
}
