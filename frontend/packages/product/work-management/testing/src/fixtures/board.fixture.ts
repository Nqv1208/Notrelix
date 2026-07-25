import type { Board, BoardGroup, FieldDefinition } from '@notrelix/work-management-core';

const defaultFields: FieldDefinition[] = [
  {
    id: 'field-title',
    boardId: 'board-test',
    name: 'Task',
    fieldType: 'text',
    options: [],
    position: 1,
    isHidden: false,
    isSystemField: true,
  },
  {
    id: 'field-status',
    boardId: 'board-test',
    name: 'Status',
    fieldType: 'select',
    options: [
      { id: 'status-todo', label: 'To Do', color: '#676879' },
      { id: 'status-doing', label: 'Doing', color: '#6161ff' },
      { id: 'status-done', label: 'Done', color: '#1aae39' },
    ],
    position: 2,
    isHidden: false,
    isSystemField: true,
  },
  {
    id: 'field-priority',
    boardId: 'board-test',
    name: 'Priority',
    fieldType: 'select',
    options: [
      { id: 'priority-low', label: 'Low', color: '#676879' },
      { id: 'priority-high', label: 'High', color: '#f64932' },
    ],
    position: 3,
    isHidden: false,
    isSystemField: true,
  },
];

export function boardFixture(overrides?: Partial<Board>): Board {
  return {
    id: 'board-test',
    workspaceId: 'workspace-test',
    title: 'Test Board',
    description: 'A test board for fixtures',
    background: { type: 'color', value: '#6161ff' },
    visibility: 'workspace',
    isArchived: false,
    fieldDefinitions: defaultFields,
    members: [],
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    ...overrides,
  };
}

export function boardGroupFixture(
  overrides?: Partial<BoardGroup>,
): BoardGroup {
  return {
    id: 'group-test',
    title: 'Test Group',
    color: '#676879',
    position: 1,
    isCollapsed: false,
    cards: [],
    ...overrides,
  };
}
