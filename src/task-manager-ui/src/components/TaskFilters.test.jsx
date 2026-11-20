import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import TaskFilters from './TaskFilters.jsx';

const renderFilters = (value = { status: '', priority: '' }) => {
  const onChange = vi.fn();
  const onClear = vi.fn();
  render(<TaskFilters value={value} onChange={onChange} onClear={onClear} />);
  return { onChange, onClear };
};

describe('TaskFilters', () => {
  it('offers every status and priority plus an "all" option', () => {
    renderFilters();

    const status = screen.getByLabelText('Status');
    const priority = screen.getByLabelText('Priority');
    expect([...status.options].map((o) => o.value)).toEqual(['', 'Todo', 'InProgress', 'Done']);
    expect([...priority.options].map((o) => o.value)).toEqual(['', 'Critical', 'High', 'Medium', 'Low']);
  });

  it('reports a status change with the API value, keeping the priority', async () => {
    const { onChange } = renderFilters({ status: '', priority: 'High' });

    await userEvent.selectOptions(screen.getByLabelText('Status'), 'In progress');

    expect(onChange).toHaveBeenCalledWith({ status: 'InProgress', priority: 'High' });
  });

  it('reports a priority change, keeping the status', async () => {
    const { onChange } = renderFilters({ status: 'Done', priority: '' });

    await userEvent.selectOptions(screen.getByLabelText('Priority'), 'Critical');

    expect(onChange).toHaveBeenCalledWith({ status: 'Done', priority: 'Critical' });
  });

  it('disables Clear filters when nothing is filtered', () => {
    renderFilters();

    expect(screen.getByRole('button', { name: 'Clear filters' })).toBeDisabled();
  });

  it('calls onClear when Clear filters is pressed', async () => {
    const { onClear } = renderFilters({ status: 'Todo', priority: '' });

    await userEvent.click(screen.getByRole('button', { name: 'Clear filters' }));

    expect(onClear).toHaveBeenCalledTimes(1);
  });
});
