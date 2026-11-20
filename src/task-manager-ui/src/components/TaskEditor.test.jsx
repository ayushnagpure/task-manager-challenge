import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../App.jsx';
import { ApiError } from '../api/ApiError.js';
import { createTask, getTask, listTasks, updateTask } from '../api/tasksApi.js';

vi.mock('../api/tasksApi.js', () => ({
  listTasks: vi.fn(),
  getTask: vi.fn(),
  createTask: vi.fn(),
  updateTask: vi.fn(),
}));

const TASK = {
  id: 1,
  title: 'Fix login outage',
  description: 'Users get 500s',
  status: 'Todo',
  priority: 'Critical',
  assignedTo: 'Dana',
  createdDate: '2026-01-05T08:00:00+00:00',
  modifiedDate: '2026-01-05T08:00:00+00:00',
  rowVersion: 'AAAAAAAAB9E=',
};

const never = () => new Promise(() => {});

beforeEach(() => {
  vi.mocked(listTasks).mockReset().mockResolvedValue([TASK]);
  vi.mocked(getTask).mockReset();
  vi.mocked(createTask).mockReset();
  vi.mocked(updateTask).mockReset();
});

async function renderApp() {
  render(<App />);
  await screen.findByText('Fix login outage');
}

/** Field lookups scoped to the dialog (the page's filters also have Status/Priority labels). */
function fields(dialog) {
  const d = within(dialog);
  return {
    title: d.getByLabelText(/^Title/),
    description: d.getByLabelText(/^Description/),
    status: d.getByLabelText(/^Status/),
    priority: d.getByLabelText(/^Priority/),
    assignedTo: d.getByLabelText(/^Assigned to/),
  };
}

async function openCreate() {
  await userEvent.click(screen.getByRole('button', { name: 'New task' }));
  return screen.getByRole('dialog', { name: 'New task' });
}

async function openEdit() {
  await userEvent.click(screen.getByRole('button', { name: 'Edit Fix login outage' }));
  const dialog = screen.getByRole('dialog', { name: 'Edit task' });
  await waitFor(() => expect(within(dialog).getByLabelText(/^Title/)).toHaveValue('Fix login outage'));
  return dialog;
}

describe('create', () => {
  it('sends the right body, closes, refreshes the list and returns focus', async () => {
    await renderApp();
    vi.mocked(createTask).mockResolvedValue({ ...TASK, id: 16, title: 'Write docs', rowVersion: 'AAAAAAAAB9M=' });
    const dialog = await openCreate();
    const f = fields(dialog);

    await userEvent.type(f.title, 'Write docs');
    await userEvent.selectOptions(f.priority, 'High');
    await userEvent.type(f.assignedTo, 'Alice');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create task' }));

    expect(createTask).toHaveBeenCalledTimes(1);
    expect(createTask).toHaveBeenCalledWith({
      title: 'Write docs',
      description: null,
      status: 'Todo',
      priority: 'High',
      assignedTo: 'Alice',
    });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(listTasks).toHaveBeenCalledTimes(2);
    expect(screen.getByRole('button', { name: 'New task' })).toHaveFocus();
  });

  it('blocks an empty title on the client and explains why next to the field', async () => {
    await renderApp();
    const dialog = await openCreate();

    await userEvent.click(within(dialog).getByRole('button', { name: 'Create task' }));

    const { title } = fields(dialog);
    expect(createTask).not.toHaveBeenCalled();
    expect(title).toHaveAttribute('aria-invalid', 'true');
    expect(title).toHaveAccessibleDescription('Title is required.');
    expect(title).toHaveFocus();
  });

  it('shows server field errors next to the right inputs and _form errors at the top', async () => {
    await renderApp();
    vi.mocked(createTask).mockRejectedValue(
      new ApiError({
        status: 400,
        title: 'One or more validation errors occurred.',
        fieldErrors: {
          title: ['Title must be at most 200 characters.'],
          priority: ['Priority must be one of: Low, Medium, High, Critical.'],
          assignedTo: ['AssignedTo must be at most 100 characters.'],
          _form: ['The input was not valid.'],
        },
      }),
    );
    const dialog = await openCreate();
    const f = fields(dialog);

    await userEvent.type(f.title, 'Looks fine to the client');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create task' }));

    await waitFor(() => expect(f.title).toHaveAccessibleDescription('Title must be at most 200 characters.'));
    expect(f.priority).toHaveAccessibleDescription('Priority must be one of: Low, Medium, High, Critical.');
    expect(f.assignedTo).toHaveAccessibleDescription('AssignedTo must be at most 100 characters.');
    expect(f.description).not.toHaveAttribute('aria-invalid');
    expect(within(dialog).getByRole('alert')).toHaveTextContent('The input was not valid.');
    expect(f.title).toHaveFocus();

    // Editing a field clears its server error.
    await userEvent.type(f.title, '!');
    expect(f.title).not.toHaveAttribute('aria-invalid');
    expect(f.priority).toHaveAttribute('aria-invalid', 'true');
  });
});

describe('edit', () => {
  it('loads the task fresh and sends every field with the rowVersion', async () => {
    await renderApp();
    vi.mocked(getTask).mockResolvedValue(TASK);
    const updated = { ...TASK, title: 'Fix login outage now', description: null, rowVersion: 'AAAAAAAAB9I=' };
    vi.mocked(updateTask).mockResolvedValue(updated);
    vi.mocked(listTasks).mockReturnValue(never()); // the post-save reload stays pending

    const dialog = await openEdit();
    expect(getTask).toHaveBeenCalledWith(1, { signal: expect.anything() });
    const f = fields(dialog);

    await userEvent.type(f.title, ' now');
    await userEvent.clear(f.description);
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save changes' }));

    expect(updateTask).toHaveBeenCalledWith(1, {
      title: 'Fix login outage now',
      description: null,
      status: 'Todo',
      priority: 'Critical',
      assignedTo: 'Dana',
      rowVersion: 'AAAAAAAAB9E=',
    });

    // The cached row is replaced immediately (the reload has not returned yet), and focus is back on its Edit button.
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.getByText('Fix login outage now')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Edit Fix login outage now' })).toHaveFocus();
    expect(listTasks).toHaveBeenCalledTimes(2);
  });

  it('on 409 keeps the edits, then loads the latest version and lists the unsaved edits', async () => {
    await renderApp();
    const latest = { ...TASK, title: 'Fix login outage (Bob)', assignedTo: 'Bob', rowVersion: 'AAAAAAAAB9I=' };
    vi.mocked(getTask).mockResolvedValueOnce(TASK).mockResolvedValueOnce(latest);
    vi.mocked(updateTask)
      .mockRejectedValueOnce(
        new ApiError({
          status: 409,
          title: 'Conflict',
          detail: 'The task was changed by someone else. Reload it and try again.',
        }),
      )
      .mockResolvedValueOnce({ ...latest, rowVersion: 'AAAAAAAAB9M=' });

    const dialog = await openEdit();
    const f = fields(dialog);
    await userEvent.clear(f.title);
    await userEvent.type(f.title, 'My title');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save changes' }));

    // Conflict: message shown, the user's edit is still in the form, Save is held until they load the latest.
    const notice = await within(dialog).findByText('Someone else changed this task after you opened it.');
    expect(notice).toBeInTheDocument();
    expect(f.title).toHaveValue('My title');
    expect(within(dialog).getByRole('button', { name: 'Save changes' })).toBeDisabled();
    expect(within(dialog).getByRole('button', { name: 'Load latest version' })).toHaveFocus();

    await userEvent.click(within(dialog).getByRole('button', { name: 'Load latest version' }));

    // The form now shows Bob's version; the user's edit is listed so it isn't lost.
    const title = await waitFor(() => {
      const input = within(dialog).getByLabelText(/^Title/);
      expect(input).toHaveValue('Fix login outage (Bob)');
      return input;
    });
    expect(within(dialog).getByLabelText(/^Assigned to/)).toHaveValue('Bob');
    const edits = within(dialog).getByText('Your unsaved edits').closest('details');
    expect(edits).toHaveTextContent('Title');
    expect(edits).toHaveTextContent('My title');
    expect(title).toHaveFocus();

    await userEvent.click(within(dialog).getByRole('button', { name: 'Save changes' }));

    expect(updateTask).toHaveBeenLastCalledWith(1, expect.objectContaining({ title: 'Fix login outage (Bob)', rowVersion: 'AAAAAAAAB9I=' }));
  });

  it('says so when the task was deleted before it could be loaded', async () => {
    await renderApp();
    vi.mocked(getTask).mockRejectedValue(new ApiError({ status: 404, title: 'Resource not found' }));

    await userEvent.click(screen.getByRole('button', { name: 'Edit Fix login outage' }));

    const dialog = screen.getByRole('dialog', { name: 'Edit task' });
    expect(await within(dialog).findByText('This task no longer exists. It may have been deleted.')).toBeInTheDocument();

    await userEvent.click(within(dialog).getByRole('button', { name: 'Close' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(listTasks).toHaveBeenCalledTimes(2); // the list refreshes so the deleted task disappears
  });

  it('sends only one request however often Save is triggered', async () => {
    await renderApp();
    vi.mocked(getTask).mockResolvedValue(TASK);
    vi.mocked(updateTask).mockReturnValue(never());

    const dialog = await openEdit();
    const save = within(dialog).getByRole('button', { name: 'Save changes' });
    await userEvent.click(save);

    const saving = within(dialog).getByRole('button', { name: 'Saving…' });
    expect(saving).toBeDisabled();
    await userEvent.click(saving);
    fireEvent.submit(dialog.querySelector('form'));
    fireEvent.submit(dialog.querySelector('form'));

    expect(updateTask).toHaveBeenCalledTimes(1);

    // Escape is ignored while saving.
    await userEvent.keyboard('{Escape}');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});

describe('keyboard', () => {
  it('can be opened, filled in order, and closed without a mouse', async () => {
    await renderApp();

    await userEvent.tab();
    const newTask = screen.getByRole('button', { name: 'New task' });
    expect(newTask).toHaveFocus();

    await userEvent.keyboard('{Enter}');
    const dialog = screen.getByRole('dialog', { name: 'New task' });
    const f = fields(dialog);
    const d = within(dialog);
    expect(f.title).toHaveFocus();

    const order = [
      f.description,
      f.status,
      f.priority,
      f.assignedTo,
      d.getByRole('button', { name: 'Cancel' }),
      d.getByRole('button', { name: 'Create task' }),
      d.getByRole('button', { name: 'Close dialog' }), // wraps to the top of the dialog
      f.title,
    ];
    for (const element of order) {
      await userEvent.tab();
      expect(element).toHaveFocus();
    }

    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(newTask).toHaveFocus();
  });

  it('submits with Enter from the title field', async () => {
    await renderApp();
    vi.mocked(createTask).mockResolvedValue({ ...TASK, id: 17, title: 'Keyboard task' });

    screen.getByRole('button', { name: 'New task' }).focus();
    await userEvent.keyboard('{Enter}');
    await userEvent.keyboard('Keyboard task{Enter}');

    expect(createTask).toHaveBeenCalledWith(expect.objectContaining({ title: 'Keyboard task', status: 'Todo', priority: 'Medium' }));
  });
});
