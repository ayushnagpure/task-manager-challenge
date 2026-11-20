import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  createTask,
  deleteTask,
  getSummary,
  getTask,
  listTasks,
  updateTask,
  updateTaskStatus,
} from './tasksApi.js';

let fetchMock;

beforeEach(() => {
  vi.stubEnv('VITE_API_BASE_URL', 'http://api.test');
  // A Response body can only be read once, so build a fresh one for every call.
  fetchMock = vi.fn(async () => new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }));
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

const lastCall = () => {
  const [url, init] = fetchMock.mock.calls.at(-1);
  return { url, method: init.method, body: init.body === undefined ? undefined : JSON.parse(init.body) };
};

describe('listTasks', () => {
  it('requests all tasks when no filters are set', async () => {
    await listTasks({ status: '', priority: '' });
    expect(lastCall()).toMatchObject({ url: 'http://api.test/api/tasks', method: 'GET' });
  });

  it('sends only the filters that are set', async () => {
    await listTasks({ status: 'Done', priority: '' });
    expect(lastCall().url).toBe('http://api.test/api/tasks?status=Done');

    await listTasks({ status: '', priority: 'Critical' });
    expect(lastCall().url).toBe('http://api.test/api/tasks?priority=Critical');

    await listTasks({ status: 'Todo', priority: 'High' });
    expect(lastCall().url).toBe('http://api.test/api/tasks?status=Todo&priority=High');
  });

  it('works with no arguments', async () => {
    await listTasks();
    expect(lastCall().url).toBe('http://api.test/api/tasks');
  });
});

describe('other endpoints', () => {
  it('getTask', async () => {
    await getTask(7);
    expect(lastCall()).toMatchObject({ url: 'http://api.test/api/tasks/7', method: 'GET' });
  });

  it('createTask', async () => {
    await createTask({ title: 'New', priority: 'Low' });
    expect(lastCall()).toEqual({ url: 'http://api.test/api/tasks', method: 'POST', body: { title: 'New', priority: 'Low' } });
  });

  it('updateTask', async () => {
    const body = { title: 'Edit', status: 'Done', priority: 'High', rowVersion: 'AAAAAAAAB9E=' };
    await updateTask(3, body);
    expect(lastCall()).toEqual({ url: 'http://api.test/api/tasks/3', method: 'PUT', body });
  });

  it('updateTaskStatus', async () => {
    await updateTaskStatus(3, 'InProgress');
    expect(lastCall()).toEqual({ url: 'http://api.test/api/tasks/3/status', method: 'PATCH', body: { status: 'InProgress' } });
  });

  it('deleteTask', async () => {
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    await expect(deleteTask(3)).resolves.toBeNull();
    expect(lastCall()).toMatchObject({ url: 'http://api.test/api/tasks/3', method: 'DELETE' });
  });

  it('getSummary', async () => {
    await getSummary();
    expect(lastCall()).toMatchObject({ url: 'http://api.test/api/tasks/summary', method: 'GET' });
  });
});
