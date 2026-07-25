export interface BoardPatch {
  type: string;
  boardId: string;
  fieldId?: string;
  itemId?: string;
  groupId?: string;
  value?: unknown;
  timestamp: string;
}

export function createBoardPatch(
  overrides?: Partial<BoardPatch>,
): BoardPatch {
  return {
    type: 'cell.updated',
    boardId: 'board-test',
    fieldId: 'field-status',
    itemId: 'card-test',
    groupId: 'group-test',
    value: 'status-doing',
    timestamp: new Date().toISOString(),
    ...overrides,
  };
}
