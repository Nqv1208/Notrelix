/**
 * Board-related realtime event types
 *
 * Typed events for board collaboration and updates.
 */

export type BoardPatchEvent = {
  type: "board.patch";
  payload: {
    boardId: string;
    changes: unknown;
    userId: string;
    timestamp: number;
  };
};

export type BoardPresenceEvent = {
  type: "board.presence";
  payload: {
    boardId: string;
    userId: string;
    action: "join" | "leave";
    cursor?: { x: number; y: number };
  };
};

export type BoardEvent = BoardPatchEvent | BoardPresenceEvent;
