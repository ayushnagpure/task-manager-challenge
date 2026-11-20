import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../App.jsx';
import { ApiError } from '../api/ApiError.js';
import { deleteTask, getSummary, listTasks, updateTaskStatus } from '../api/tasksApi.js';

vi.mock('../api/tasksApi.js', () => ({
  listTasks: vi.fn(),
  getTask: vi.fn(),
  createTask: vi.fn(),
  updateTask: vi.fn(),
  updateTaskStatus: vi.fn(),
  deleteTask: vi.fn(),
  getSummary: vi.fn(),
}));

const TASK = {
  id: 1,
  title: 'Fix login outage',
  description: null,
  status: 'Todo',
  priority: 'Critical',
  assignedTo: 'Dana',
  createdDate: '2026-01-05T08:00:00+00:00',
  modifiedDate: '2026-01-05T08:00:00+00:00',
  rowVersion: 'AAAAAAAAB9E=',
};

beforeEach(() => {
  vi.mocked(listTasks).mockReset().mockResolvedValue([TASK]);
  vi.mocked(getSummary).mockReset().mockResolvedValue([]);
  vi.mocked(updateTaskStatus).mockReset();
  vi.mocked(deleteTask).mockReset();
});

async function renderApp() {
  render(<App />);
  await screen.findByText('Fix login outage');
}

const statusSelect = () => screen.getByRole('combobox', { name: 'Status for Fix login outage' });

describe('status dropdown', () => {
  it('saves through PATCH, shows the in-flight state, then keeps the new status', async () => {
    await renderApp();
    let resolvePatch;
    vi.mocked(updateTaskStatus).mockReturnValue(new Promise((resolve) => (resolvePatch = resolve)));

    await userEvent.selectOptions(statusSelect(), 'Done');

    expect(updateTaskStatus).toHaveBeenCalledWith(1, 'Done');
    expect(statusSelect()).toHaveValue('Done'); // optimistic
    expect(statusSelect()).toBeDisabled();
    expect(screen.getByText('Saving…')).toBeInTheDocument();

    vi.mocked(listTasks).mockResolvedValue([{ ...TASK, status: 'Done', rowVersion: 'AAAAAAAAB9I=' }]);
    await act(async () => resolvePatch({ ...TASK, status: 'Done', rowVersion: 'AAAAAAAAB9I=' }));

    await waitFor(() => expect(statusSelect()).toBeEnabled());
    expect(statusSelect()).toHaveValue('Done');
    expect(screen.queryByText('Saving…')).not.toBeInTheDocument();
    expect(listTasks).toHaveBeenCalledTimes(2); // re-applies filters
    expect(getSummary).toHaveBeenCalledTimes(2); // counts refreshed
  });

  it('reverts and explains when the PATCH fails, without touching the list', async () => {
    await renderApp();
    vi.mocked(updateTaskStatus).mockRejectedValue(
      new ApiError({ status: 500, title: 'Something went wrong on the server.' }),
    );

    await userEvent.selectOptions(statusSelect(), 'InProgress');

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Couldn\'t change the status of “Fix login outage”. Something went wrong on the server.');
    expect(statusSelect()).toHaveValue('Todo'); // reverted
    expect(statusSelect()).toBeEnabled();
    expect(statusSelect()).toHaveAccessibleDescription(/Couldn't change the status/);
    expect(updateTaskStatus).toHaveBeenCalledTimes(1);
    expect(listTasks).toHaveBeenCalledTimes(1);
    expect(getSummary).toHaveBeenCalledTimes(1); // nothing changed, so nothing refreshed

    await userEvent.click(screen.getByRole('button', { name: 'Dismiss error for Fix login outage' }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('says the task is gone and refreshes when the PATCH returns 404', async () => {
    await renderApp();
    vi.mocked(updateTaskStatus).mockRejectedValue(new ApiError({ status: 404, title: 'Resource not found' }));
    vi.mocked(listTasks).mockResolvedValue([]);

    await userEvent.selectOptions(statusSelect(), 'Done');

    expect(await screen.findByText('“Fix login outage” no longer exists. It may have been deleted.')).toBeInTheDocument();
    expect(screen.queryByRole('combobox', { name: 'Status for Fix login outage' })).not.toBeInTheDocument();
    expect(listTasks).toHaveBeenCalledTimes(2);
  });
});

describe('delete', () => {
  const deleteButton = () => screen.getByRole('button', { name: 'Delete Fix login outage' });

  it('asks for confirmation; Cancel does nothing and returns focus to the Delete button', async () => {
    await renderApp();

    await userEvent.click(deleteButton());
    const dialog = screen.getByRole('dialog', { name: 'Delete task?' });
    expect(dialog).toHaveTextContent('“Fix login outage” will be removed from the task list.');
    expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus(); // safe default

    await userEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(deleteTask).not.toHaveBeenCalled();
    expect(deleteButton()).toHaveFocus();
  });

  it('deletes on confirm, removes the row, refreshes and focuses the list heading', async () => {
    await renderApp();
    vi.mocked(deleteTask).mockResolvedValue(null);
    vi.mocked(listTasks).mockResolvedValue([]);

    await userEvent.click(deleteButton());
    await userEvent.click(screen.getByRole('button', { name: 'Delete task' }));

    expect(deleteTask).toHaveBeenCalledWith(1);
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.queryByText('Fix login outage')).not.toBeInTheDocument();
    expect(listTasks).toHaveBeenCalledTimes(2);
    expect(getSummary).toHaveBeenCalledTimes(2);
    expect(screen.getByRole('heading', { name: 'Tasks' })).toHaveFocus();
  });

  it('treats a 404 as already deleted: closes, refreshes and says so', async () => {
    await renderApp();
    vi.mocked(deleteTask).mockRejectedValue(new ApiError({ status: 404, title: 'Resource not found' }));
    vi.mocked(listTasks).mockResolvedValue([]);

    await userEvent.click(deleteButton());
    await userEvent.click(screen.getByRole('button', { name: 'Delete task' }));

    expect(await screen.findByText('“Fix login outage” had already been deleted.')).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(listTasks).toHaveBeenCalledTimes(2);
    expect(getSummary).toHaveBeenCalledTimes(2);

    await userEvent.click(screen.getByRole('button', { name: 'Dismiss notice' }));
    expect(screen.queryByText('“Fix login outage” had already been deleted.')).not.toBeInTheDocument();
  });

  it('keeps the dialog open with the error when the delete fails for another reason', async () => {
    await renderApp();
    vi.mocked(deleteTask).mockRejectedValue(new ApiError({ status: 500, title: 'Something went wrong on the server.' }));

    await userEvent.click(deleteButton());
    await userEvent.click(screen.getByRole('button', { name: 'Delete task' }));

    const dialog = screen.getByRole('dialog', { name: 'Delete task?' });
    expect(await within(dialog).findByRole('alert')).toHaveTextContent('Something went wrong on the server.');
    expect(within(dialog).getByRole('button', { name: 'Delete task' })).toBeEnabled(); // can retry
    expect(screen.getByText('Fix login outage', { selector: '.task-row__title' })).toBeInTheDocument();
  });

  it('ignores Escape while the delete is in flight', async () => {
    await renderApp();
    vi.mocked(deleteTask).mockReturnValue(new Promise(() => {}));

    await userEvent.click(deleteButton());
    await userEvent.click(screen.getByRole('button', { name: 'Delete task' }));
    await userEvent.keyboard('{Escape}');

    expect(screen.getByRole('dialog', { name: 'Delete task?' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Deleting…' })).toBeDisabled();
    expect(deleteTask).toHaveBeenCalledTimes(1);
  });
});
