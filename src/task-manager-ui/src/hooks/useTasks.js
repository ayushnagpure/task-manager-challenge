import { useCallback, useEffect, useState } from 'react';
import { listTasks } from '../api/tasksApi.js';

/**
 * Loads the task list for the given filters and reloads whenever they change.
 * A newer request aborts the previous one, so a slow old response can never overwrite newer results.
 * Previously loaded tasks stay visible while a refresh is in flight (`loading` is true).
 *
 * @param {import('../api/tasksApi.js').TaskFilters} filters
 * @returns {{
 *   tasks: import('../api/tasksApi.js').Task[],
 *   loading: boolean,
 *   loaded: boolean,
 *   error: import('../api/ApiError.js').ApiError | null,
 *   reload: () => void,
 * }}
 */
export function useTasks(filters) {
  const { status = '', priority = '' } = filters;
  const [state, setState] = useState({ tasks: [], loading: true, loaded: false, error: null });
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setState((previous) => ({ ...previous, loading: true, error: null }));

    listTasks({ status, priority }, { signal: controller.signal })
      .then((tasks) => {
        if (!controller.signal.aborted) {
          setState({ tasks, loading: false, loaded: true, error: null });
        }
      })
      .catch((error) => {
        if (controller.signal.aborted || error?.name === 'AbortError') {
          return;
        }

        setState((previous) => ({ ...previous, loading: false, error }));
      });

    return () => controller.abort();
  }, [status, priority, reloadToken]);

  const reload = useCallback(() => setReloadToken((token) => token + 1), []);

  return { ...state, reload };
}
