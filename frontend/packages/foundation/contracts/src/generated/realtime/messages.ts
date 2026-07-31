/**
 * Generated from artifacts/contracts/realtime.v1.json
 * DO NOT EDIT.
 */

export interface BoardItemCreatedPayload {
  "boardId": string;
  "itemId": string;
  "title": string;
}

export interface BoardItemMovedPayload {
  "itemId": string;
  "position": number;
  "targetGroupId": string;
}

export interface BoardItemUpdatedPayload {
  "field": string;
  "itemId": string;
  "value"?: string;
}

export type GeneratedRealtimeMessage =
  | { eventType: "board.item.created"; payload: BoardItemCreatedPayload }
  | { eventType: "board.item.moved"; payload: BoardItemMovedPayload }
  | { eventType: "board.item.updated"; payload: BoardItemUpdatedPayload };

export type RealtimeEventMessage = GeneratedRealtimeMessage;
