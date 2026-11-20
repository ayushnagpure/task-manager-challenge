import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/ApiError.js';
import TaskSummary from './TaskSummary.jsx';

const ITEMS = [
  { status: 'Todo', priority: 'High', count: 2 },
  { status: 'Todo', priority: 'Low', count: 1 },
  { status: 'InProgress', priority: 'Critical', count: 1 },
  { status: 'Done', priority: 'Critical', count: 3 },
];

const renderSummary = (props) =>
  render(<TaskSummary items={[]} loading={false} loaded error={null} onRetry={vi.fn()} {...props} />);

/** Cell values of the row whose header is `name` (columns: Critical, High, Medium, Low, Total). */
const rowCounts = (name) =>
  within(screen.getByRole('rowheader', { name }).closest('tr'))
    .getAllByRole('cell')
    .map((cell) => Number(cell.textContent));

describe('TaskSummary', () => {
  it('renders a status x priority matrix with zeros filled in and totals', () => {
    renderSummary({ items: ITEMS });

    expect(screen.getAllByRole('columnheader').map((th) => th.textContent)).toEqual([
      'Status',
      '▲▲Critical',
      '▲High',
      '●Medium',
      '▼Low',
      'Total',
    ]);
    expect(rowCounts('To do')).toEqual([0, 2, 0, 1, 3]);
    expect(rowCounts('In progress')).toEqual([1, 0, 0, 0, 1]);
    expect(rowCounts('Done')).toEqual([3, 0, 0, 0, 3]);
    expect(rowCounts('Total')).toEqual([4, 2, 0, 1, 7]);
  });

  it('says the counts ignore the list filters', () => {
    renderSummary({ items: ITEMS });

    expect(screen.getByRole('table')).toHaveAccessibleName(/Not affected by the list filters/);
  });

  it('shows an empty state when there are no active tasks', () => {
    renderSummary({ items: [] });

    expect(screen.getByText('No active tasks yet.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows a loading message before the first response', () => {
    renderSummary({ loaded: false, loading: true });

    expect(screen.getByRole('status')).toHaveTextContent('Loading summary…');
  });

  it('shows the error with a Retry button', async () => {
    const onRetry = vi.fn();
    renderSummary({ error: new ApiError({ status: 500, title: 'Something went wrong on the server.' }), onRetry });

    expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong on the server.');
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
