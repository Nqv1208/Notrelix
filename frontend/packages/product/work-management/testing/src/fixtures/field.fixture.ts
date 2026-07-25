import type { FieldDefinition } from '@notrelix/work-management-core';

export function fieldFixture(overrides?: Partial<FieldDefinition>): FieldDefinition {
  return {
    id: 'field-test',
    boardId: 'board-test',
    name: 'Test Field',
    fieldType: 'text',
    options: [],
    position: 1,
    isHidden: false,
    isSystemField: false,
    ...overrides,
  };
}
