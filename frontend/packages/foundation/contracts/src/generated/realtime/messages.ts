/**
 * Generated Realtime Event Messages from /Users/nqvinh/Documents/projects/Notrelix/artifacts/contracts/realtime.v1.json
 * DO NOT EDIT MANUALLY.
 */

export interface BoardItemCreatedPayload {
  itemId: string;
  boardId: string;
  title: string;
}

export interface BoardItemUpdatedPayload {
  itemId: string;
  field: string;
  value?: string;
}

export interface BoardItemMovedPayload {
  itemId: string;
  targetGroupId: string;
  position: number;
}

export type RealtimeEventMessage =
  | { type: 'board.item.created'; payload: BoardItemCreatedPayload }
  | { type: 'board.item.updated'; payload: BoardItemUpdatedPayload }
  | { type: 'board.item.moved'; payload: BoardItemMovedPayload };
