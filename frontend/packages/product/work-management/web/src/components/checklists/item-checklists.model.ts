import type { Checklist } from "@notrelix/work-management-core";

/** What the section needs: the lists, what the viewer may do, and the writes. */
export interface ItemChecklistsModel {
  readonly checklists: readonly Checklist[];
  /** The viewer may add, tick, edit, delete and reorder. */
  readonly canEdit: boolean;
  /** A sub-item has no sub-items of its own, so it offers no conversion. */
  readonly canConvert: boolean;
  readonly addList: (title: string) => void;
  readonly renameList: (checklistId: string, title: string) => void;
  readonly deleteList: (checklistId: string) => void;
  readonly moveList: (checklistId: string, toIndex: number) => void;
  /** `extras`: the person and the due time (an ISO instant) the new item starts with; a draft ignores them. */
  readonly addItem: (
    checklistId: string,
    title: string,
    extras?: { assigneeId?: string | null; dueAt?: string | null },
  ) => void;
  readonly renameItem: (
    checklistId: string,
    itemId: string,
    title: string,
  ) => void;
  readonly toggleItem: (checklistId: string, itemId: string) => void;
  /** Gives an item to a person or to nobody; absent: the row has no such control. */
  readonly setItemAssignee?: (
    checklistId: string,
    itemId: string,
    assigneeId: string | null,
  ) => void;
  /** Sets an item's own due time (an ISO instant) or clears it. */
  readonly setItemDue?: (
    checklistId: string,
    itemId: string,
    dueAt: string | null,
  ) => void;
  readonly deleteItem: (checklistId: string, itemId: string) => void;
  readonly moveItem: (
    checklistId: string,
    itemId: string,
    toIndex: number,
  ) => void;
  /** Rejects when the sub-item could not be created (the item then stays as it is). */
  readonly convertItem: (checklistId: string, itemId: string) => Promise<void>;
  /** Takes the waiting delete back. */
  readonly undo: () => void;
  readonly retry: () => void;
  readonly discard: () => void;
  readonly dismissConflict: () => void;
  /** The write the server refused: its text stays on screen. */
  readonly failure: {
    readonly op: {
      readonly kind: string;
      readonly checklistId?: string;
      readonly itemId?: string;
      readonly text?: string;
    };
  } | null;
  /** Another writer changed the list first; the latest version is shown. */
  readonly conflict: boolean;
  /** The delete the user may still take back. */
  readonly pendingDelete: {
    readonly kind: "item" | "list";
    readonly text: string;
  } | null;
  /** Writes are on their way. */
  readonly busy: boolean;
}
