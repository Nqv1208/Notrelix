import type { QueryClient } from "@tanstack/react-query";
import {
  checklistProgressOf,
  type CardDetail,
  type Checklist,
  type FullBoardResponse,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";

/** How long a delete waits for "Hoàn tác" before it is sent. */
export const CHECKLIST_UNDO_MS = 5000;

export type ChecklistWriteKind =
  | "add-list"
  | "rename-list"
  | "delete-list"
  | "move-list"
  | "add-item"
  | "rename-item"
  | "toggle-item"
  | "assign-item"
  | "due-item"
  | "delete-item"
  | "move-item"
  /** The second step of "convert to sub-item": the sub-item exists, the item is still listed. */
  | "convert-cleanup";

export interface ChecklistOp {
  readonly id: string;
  readonly kind: ChecklistWriteKind;
  /** The list this write changes (absent for a new list). */
  readonly checklistId?: string;
  readonly itemId?: string;
  /** The text the user wrote, kept visible when the write fails. */
  readonly text?: string;
  /** The change as the screen shows it at once. */
  readonly apply: (lists: Checklist[]) => Checklist[];
  /** The request; `version` is the list's version at send time. */
  readonly send: (version: number | undefined) => Promise<void>;
}

export interface ChecklistFailure {
  readonly op: Pick<
    ChecklistOp,
    "id" | "kind" | "checklistId" | "itemId" | "text"
  >;
}

export interface PendingDelete {
  readonly id: string;
  readonly kind: "item" | "list";
  readonly text: string;
}

export interface ChecklistQueueSnapshot {
  readonly failure: ChecklistFailure | null;
  /** Another writer changed the list first; the list was read again. */
  readonly conflict: boolean;
  /** The delete the user may still take back. */
  readonly pendingDelete: PendingDelete | null;
  /** Writes not yet answered; ids of items still waiting for their real id. */
  readonly busy: boolean;
}

const IDLE: ChecklistQueueSnapshot = {
  failure: null,
  conflict: false,
  pendingDelete: null,
  busy: false,
};

/** A write the server refused with a version conflict (HTTP 409 as an `AppError`). */
export function isChecklistConflict(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    (error as { kind?: unknown }).kind === "conflict"
  );
}

interface Pending {
  readonly op: ChecklistOp;
  /** Puts the entry back when the user takes a delayed delete back. */
  readonly restore?: (lists: Checklist[]) => Checklist[];
  timer?: ReturnType<typeof setTimeout>;
}

/**
 * The checklist writes of one item. A list is the consistency boundary, so its
 * writes go one after another, each naming the version the last one left. The
 * screen takes each change at once; a refused write stays on screen with its
 * text, and the user retries it or discards it (never a silent revert).
 */
export class ChecklistQueue {
  private readonly listeners = new Set<() => void>();
  private snapshot: ChecklistQueueSnapshot = IDLE;
  private readonly waiting: Pending[] = [];
  private delayed: Pending | null = null;
  private running = false;
  private failed: { pending: Pending; error: unknown } | null = null;

  constructor(
    private readonly queryClient: QueryClient,
    private readonly workspaceId: string,
    private readonly boardId: string,
    private readonly cardId: string,
  ) {}

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  getSnapshot = (): ChecklistQueueSnapshot => this.snapshot;

  private publish(patch: Partial<ChecklistQueueSnapshot>): void {
    this.snapshot = {
      failure: this.failed
        ? {
            op: {
              id: this.failed.pending.op.id,
              kind: this.failed.pending.op.kind,
              checklistId: this.failed.pending.op.checklistId,
              itemId: this.failed.pending.op.itemId,
              text: this.failed.pending.op.text,
            },
          }
        : null,
      conflict: this.snapshot.conflict,
      pendingDelete: this.delayed
        ? {
            id: this.delayed.op.id,
            kind: this.delayed.op.kind === "delete-list" ? "list" : "item",
            text: this.delayed.op.text ?? "",
          }
        : null,
      busy: this.running || this.waiting.length > 0 || this.failed !== null,
      ...patch,
    };
    this.listeners.forEach((listener) => listener());
  }

  private get detailKey() {
    return wmQueryKeys.cardDetail(this.workspaceId, this.cardId);
  }

  private writeLists(change: (lists: Checklist[]) => Checklist[]): void {
    const detail = this.queryClient.getQueryData<CardDetail>(this.detailKey);
    if (!detail) return;
    const next = change(detail.checklists);
    const total = next.reduce((n, l) => n + l.items.length, 0);
    this.queryClient.setQueryData<CardDetail>(this.detailKey, {
      ...detail,
      checklists: next,
      checklistProgress: checklistProgressOf(next),
      _count: { ...detail._count, checklistItems: total },
    });
    const progress = checklistProgressOf(next);
    this.queryClient.setQueryData<FullBoardResponse | undefined>(
      wmQueryKeys.fullBoard(this.workspaceId, this.boardId),
      (board) =>
        board
          ? {
              ...board,
              groups: board.groups.map((group) => ({
                ...group,
                cards: group.cards.map((card) =>
                  card.id === this.cardId
                    ? {
                        ...card,
                        checklistProgress: progress,
                        _count: { ...card._count, checklistItems: total },
                      }
                    : card,
                ),
              })),
            }
          : board,
    );
  }

  /** The list's version as the screen last read it. */
  private versionOf(checklistId: string | undefined): number | undefined {
    if (!checklistId) return undefined;
    return this.queryClient
      .getQueryData<CardDetail>(this.detailKey)
      ?.checklists.find((list) => list.id === checklistId)?.version;
  }

  /** Shows the change now and queues the request (after `delayMs` when given). */
  schedule(
    op: ChecklistOp,
    options: {
      delayMs?: number;
      restore?: (lists: Checklist[]) => Checklist[];
    } = {},
  ): void {
    // A second delayed delete sends the first one now: one undo at a time.
    if (options.delayMs && this.delayed) this.releaseDelayed();
    this.writeLists(op.apply);
    this.snapshot = { ...this.snapshot, conflict: false };
    const pending: Pending = { op, restore: options.restore };
    if (options.delayMs) {
      this.delayed = pending;
      pending.timer = setTimeout(() => this.releaseDelayed(), options.delayMs);
      this.publish({});
      return;
    }
    this.waiting.push(pending);
    this.publish({});
    void this.drain();
  }

  /** Sends the delete that was waiting for an undo. */
  releaseDelayed(): void {
    const pending = this.delayed;
    if (!pending) return;
    clearTimeout(pending.timer);
    this.delayed = null;
    this.waiting.push(pending);
    this.publish({});
    void this.drain();
  }

  /** Takes the waiting delete back: the entry returns to where it was. */
  undo(): void {
    const pending = this.delayed;
    if (!pending) return;
    clearTimeout(pending.timer);
    this.delayed = null;
    if (pending.restore) this.writeLists(pending.restore);
    this.publish({});
  }

  private async drain(): Promise<void> {
    if (this.running || this.failed) return;
    this.running = true;
    this.publish({});
    try {
      while (this.waiting.length > 0) {
        const pending = this.waiting[0]!;
        try {
          await pending.op.send(this.versionOf(pending.op.checklistId));
        } catch (error) {
          if (isChecklistConflict(error)) {
            // Never overwrite blindly: drop what is queued and read again.
            this.waiting.length = 0;
            this.snapshot = { ...this.snapshot, conflict: true };
            break;
          }
          this.failed = { pending, error };
          return;
        }
        this.waiting.shift();
        // The write moved the list's version by one; the next one names it.
        if (pending.op.checklistId) {
          const id = pending.op.checklistId;
          this.writeLists((lists) =>
            lists.map((list) =>
              list.id === id && list.version !== undefined
                ? { ...list, version: list.version + 1 }
                : list,
            ),
          );
        }
      }
    } finally {
      this.running = false;
      if (!this.failed) await this.reconcile();
      this.publish({});
    }
  }

  /** Reads the item, its board and its history again once nothing is in flight. */
  private async reconcile(): Promise<void> {
    if (this.waiting.length > 0 || this.delayed) return;
    await Promise.all([
      this.queryClient.invalidateQueries({ queryKey: this.detailKey }),
      this.queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(this.workspaceId, this.boardId),
      }),
      this.queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardActivity(this.workspaceId, this.cardId),
      }),
    ]);
  }

  /** Sends the refused write again, then the ones behind it. */
  retry(): void {
    if (!this.failed) return;
    this.failed = null;
    this.publish({});
    void this.drain();
  }

  /** Gives up the refused write and what waits behind it; the server's list returns. */
  discard(): void {
    this.failed = null;
    this.waiting.length = 0;
    if (this.delayed) {
      clearTimeout(this.delayed.timer);
      this.delayed = null;
    }
    this.snapshot = { ...this.snapshot, conflict: false };
    this.publish({});
    void this.reconcile().then(() => this.publish({}));
  }

  dismissConflict(): void {
    this.snapshot = { ...this.snapshot, conflict: false };
    this.publish({});
  }
}

const queues = new WeakMap<QueryClient, Map<string, ChecklistQueue>>();

/** One queue per item and client, so the sheet and the page write through the same one. */
export function checklistQueueFor(
  queryClient: QueryClient,
  workspaceId: string,
  boardId: string,
  cardId: string,
): ChecklistQueue {
  let byCard = queues.get(queryClient);
  if (!byCard) {
    byCard = new Map();
    queues.set(queryClient, byCard);
  }
  const key = `${workspaceId}/${cardId}`;
  let queue = byCard.get(key);
  if (!queue) {
    queue = new ChecklistQueue(queryClient, workspaceId, boardId, cardId);
    byCard.set(key, queue);
  }
  return queue;
}
