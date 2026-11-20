import { describe, expect, it } from 'vitest';
import { changedFields, NEW_TASK_VALUES, toTaskInput, validateTask, valuesFromTask } from './taskForm.js';

const valid = { title: 'Write docs', description: '', status: 'Todo', priority: 'High', assignedTo: '' };

describe('validateTask', () => {
  it('accepts valid values', () => {
    expect(validateTask(valid)).toEqual({});
  });

  it.each(['', '   '])('requires a title (%j)', (title) => {
    expect(validateTask({ ...valid, title })).toEqual({ title: ['Title is required.'] });
  });

  it('allows a title of exactly 200 characters and rejects 201', () => {
    expect(validateTask({ ...valid, title: 'a'.repeat(200) })).toEqual({});
    expect(validateTask({ ...valid, title: 'a'.repeat(201) })).toEqual({ title: ['Title must be at most 200 characters.'] });
  });

  it('limits description to 2000 characters', () => {
    expect(validateTask({ ...valid, description: 'd'.repeat(2000) })).toEqual({});
    expect(validateTask({ ...valid, description: 'd'.repeat(2001) })).toEqual({
      description: ['Description must be at most 2000 characters.'],
    });
  });

  it('limits assignedTo to 100 characters', () => {
    expect(validateTask({ ...valid, assignedTo: 'n'.repeat(100) })).toEqual({});
    expect(validateTask({ ...valid, assignedTo: 'n'.repeat(101) })).toEqual({
      assignedTo: ['Assigned to must be at most 100 characters.'],
    });
  });

  it('requires a known priority and status', () => {
    expect(validateTask({ ...valid, priority: '' })).toEqual({ priority: ['Priority is required.'] });
    expect(validateTask({ ...valid, priority: 'Urgent' })).toEqual({
      priority: ['Priority must be one of: Low, Medium, High, Critical.'],
    });
    expect(validateTask({ ...valid, status: '' })).toEqual({ status: ['Status is required.'] });
  });

  it('reports every invalid field at once', () => {
    expect(Object.keys(validateTask({ ...valid, title: '', assignedTo: 'n'.repeat(101) }))).toEqual(['title', 'assignedTo']);
  });
});

describe('toTaskInput', () => {
  it('trims text and sends blank optional fields as null', () => {
    expect(toTaskInput({ title: '  Write docs ', description: '   ', status: 'Todo', priority: 'High', assignedTo: ' Alice ' })).toEqual({
      title: 'Write docs',
      description: null,
      status: 'Todo',
      priority: 'High',
      assignedTo: 'Alice',
    });
  });

  it('always includes every field, and rowVersion only when editing', () => {
    const create = toTaskInput(NEW_TASK_VALUES);
    expect(Object.keys(create)).toEqual(['title', 'description', 'status', 'priority', 'assignedTo']);
    expect(create).not.toHaveProperty('rowVersion');

    expect(toTaskInput(valid, 'AAAAAAAAB9E=')).toMatchObject({ rowVersion: 'AAAAAAAAB9E=' });
  });
});

describe('valuesFromTask', () => {
  it('turns null fields into empty strings for the inputs', () => {
    expect(
      valuesFromTask({ id: 1, title: 'T', description: null, status: 'Done', priority: 'Low', assignedTo: null }),
    ).toEqual({ title: 'T', description: '', status: 'Done', priority: 'Low', assignedTo: '' });
  });
});

describe('changedFields', () => {
  it('lists the fields that differ, with the new value', () => {
    const before = { ...valid, assignedTo: 'Alice' };
    const after = { ...valid, title: 'Write better docs', assignedTo: '' };

    expect(changedFields(before, after)).toEqual([
      { field: 'title', label: 'Title', value: 'Write better docs' },
      { field: 'assignedTo', label: 'Assigned to', value: '' },
    ]);
  });

  it('ignores whitespace-only differences', () => {
    expect(changedFields(valid, { ...valid, title: ' Write docs ' })).toEqual([]);
  });
});
