import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App.jsx';
import { ApiError } from './api/ApiError.js';
import { listTasks } from './api/tasksApi.js';

vi.mock('./api/tasksApi.js', () => ({ listTasks: vi.fn(), getTask: vi.fn(), createTask: vi.fn(), updateTask: vi.fn() }));

const task = (overrides) => ({
  id: 1,
  title: 'Fix login outage',
  description: 'Users get 500s',
  status: 'Todo',
  priority: 'Critical',
  assignedTo: 'Dana',
  createdDate: '2026-01-05T08:00:00+00:00',
  modifiedDate: '2026-01-05T08:00:00+00:00',
  rowVersion: 'AAAAAAAAB9E=',
  ...overrides,
});

const filtersOf = (call) => call[0];

beforeEach(() => {
  listTasks.mockReset();
});

describe('App task list', () => {
  it('loads all tasks on start and shows them', async () => {
    listTasks.mockResolvedValue([task(), task({ id: 2, title: 'Archive files', priority: 'Low', assignedTo: null })]);

    render(<App />);

    expect(await screen.findByText('Fix login outage')).toBeInTheDocument();
    expect(screen.getByText('Archive files')).toBeInTheDocument();
    expect(screen.getByText('Unassigned')).toBeInTheDocument();
    expect(screen.getByText('2 tasks')).toBeInTheDocument();
    expect(filtersOf(listTasks.mock.calls[0])).toEqual({ status: '', priority: '' });
    expect(listTasks.mock.calls[0][1]).toEqual({ signal: expect.anything() });
  });

  it('calls the API with the selected filters', async () => {
    listTasks.mockResolvedValue([task()]);
    render(<App />);
    await screen.findByText('Fix login outage');

    await userEvent.selectOptions(screen.getByLabelText('Status'), 'Done');
    await waitFor(() => expect(filtersOf(listTasks.mock.lastCall)).toEqual({ status: 'Done', priority: '' }));

    await userEvent.selectOptions(screen.getByLabelText('Priority'), 'High');
    await waitFor(() => expect(filtersOf(listTasks.mock.lastCall)).toEqual({ status: 'Done', priority: 'High' }));

    await userEvent.click(screen.getByRole('button', { name: 'Clear filters' }));
    await waitFor(() => expect(filtersOf(listTasks.mock.lastCall)).toEqual({ status: '', priority: '' }));
  });

  it('shows a loading message until the first response arrives', () => {
    listTasks.mockReturnValue(new Promise(() => {}));

    render(<App />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading tasks…');
  });

  it('shows "No tasks yet" when there are no tasks at all', async () => {
    listTasks.mockResolvedValue([]);

    render(<App />);

    expect(await screen.findByText('No tasks yet')).toBeInTheDocument();
  });

  it('shows a filtered empty state whose button clears the filters', async () => {
    listTasks.mockResolvedValueOnce([task()]).mockResolvedValueOnce([]).mockResolvedValue([task()]);
    render(<App />);
    await screen.findByText('Fix login outage');

    await userEvent.selectOptions(screen.getByLabelText('Status'), 'Done');
    const empty = await screen.findByText('No tasks match these filters');
    await userEvent.click(within(empty.parentElement).getByRole('button', { name: 'Clear filters' }));

    expect(await screen.findByText('Fix login outage')).toBeInTheDocument();
    expect(filtersOf(listTasks.mock.lastCall)).toEqual({ status: '', priority: '' });
  });

  it('shows the API error and retries on request', async () => {
    listTasks
      .mockRejectedValueOnce(
        new ApiError({
          kind: 'network',
          title: "Can't reach the API at http://localhost:5157.",
          detail: 'Check that the API is running.',
        }),
      )
      .mockResolvedValue([task()]);

    render(<App />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent("Can't reach the API at http://localhost:5157.");
    expect(alert).toHaveTextContent('Check that the API is running.');

    await userEvent.click(within(alert).getByRole('button', { name: 'Retry' }));

    expect(await screen.findByText('Fix login outage')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(listTasks).toHaveBeenCalledTimes(2);
  });
});
