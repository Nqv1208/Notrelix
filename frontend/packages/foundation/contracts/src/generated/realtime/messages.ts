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

export interface ChatMessageCreatedPayload {
  "authorId": string;
  "content": string | null;
  "contentOmitted": boolean;
  "createdAt": string | null;
  "deleted": boolean;
  "editedAt": string | null;
  "id": string;
  "kind": "user" | "system";
  "replyToId": string | null;
  "sequence": number;
  "version": number;
}

export interface ChatMessageDeletedPayload {
  "authorId": string;
  "content": string | null;
  "contentOmitted": boolean;
  "createdAt": string | null;
  "deleted": boolean;
  "editedAt": string | null;
  "id": string;
  "kind": "user" | "system";
  "replyToId": string | null;
  "sequence": number;
  "version": number;
}

export interface ChatMessageUpdatedPayload {
  "authorId": string;
  "content": string | null;
  "contentOmitted": boolean;
  "createdAt": string | null;
  "deleted": boolean;
  "editedAt": string | null;
  "id": string;
  "kind": "user" | "system";
  "replyToId": string | null;
  "sequence": number;
  "version": number;
}

export interface ChatUnreadChangedPayload {
  "mentions": number;
  "spaceId": string;
  "unread": number;
}

export type GeneratedRealtimeMessage =
  | { eventType: "board.item.created"; payload: BoardItemCreatedPayload }
  | { eventType: "board.item.moved"; payload: BoardItemMovedPayload }
  | { eventType: "board.item.updated"; payload: BoardItemUpdatedPayload }
  | { eventType: "chat.message.created"; payload: ChatMessageCreatedPayload }
  | { eventType: "chat.message.deleted"; payload: ChatMessageDeletedPayload }
  | { eventType: "chat.message.updated"; payload: ChatMessageUpdatedPayload }
  | { eventType: "chat.unread.changed"; payload: ChatUnreadChangedPayload };

export type RealtimeEventMessage = GeneratedRealtimeMessage;
