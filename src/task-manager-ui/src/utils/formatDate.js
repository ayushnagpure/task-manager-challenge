const formatter = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

/**
 * Formats an ISO date from the API in the user's locale and time zone, e.g. "20 Nov 2026, 10:00".
 * @param {string | null | undefined} iso
 * @returns {string}
 */
export function formatDateTime(iso) {
  if (!iso) {
    return '';
  }

  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? '' : formatter.format(date);
}
