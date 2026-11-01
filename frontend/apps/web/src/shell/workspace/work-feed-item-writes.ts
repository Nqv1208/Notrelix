/**
 * The work feed's item writes, expressed on the Work Management contract that
 * exists on the producer: create an item in a Board group, assign a member,
 * set a due date, change status, stop following. There is no "my work"
 * write route; My work and Home compose these per-item calls.
 *
 * Plain functions over the API client so the sequences are provable without
 * React (`useWorkFeedWrites` wires them to the cache invalidation).
 */
import type { NotrelixClient } from "@notrelix/contracts";

export type FeedApi = NotrelixClient["api"];

export type WorkBucket = "todo" | "doing" | "review" | "done";

export type BulkAction =
  | { readonly type: "set-due"; readonly dueAt: string | null }
  | { readonly type: "set-status"; readonly bucket: WorkBucket }
  | { readonly type: "unfollow" }
  | { readonly type: "assign"; readonly assigneeId: string };

export interface BulkResult {
  readonly results: readonly {
    readonly itemId: string;
    readonly ok: boolean;
    readonly error: "not-found" | "invalid" | null;
  }[];
}

const QUIET = { skipGlobalErrorToast: true } as const;

/** One idempotency key per user-initiated attempt. */
const newKey = () => crypto.randomUUID();

interface FullBoardLike {
  readonly columns?: readonly {
    readonly fieldType?: string | null;
    readonly isDeleted?: boolean;
    readonly options?: readonly {
      readonly id?: string;
      readonly category?: string | null;
    }[];
  }[];
  readonly lists?: readonly {
    readonly id?: string;
    readonly isArchived?: boolean;
  }[];
}

const BUCKET_OF_CATEGORY: Readonly<Record<string, WorkBucket>> = {
  todo: "todo",
  inprogress: "doing",
  doing: "doing",
  review: "review",
  done: "done",
};

function bucketOfCategory(category: string | null | undefined): WorkBucket {
  const key = (category ?? "todo").toLowerCase().replace(/[^a-z]/g, "");
  return BUCKET_OF_CATEGORY[key] ?? "todo";
}

/**
 * The version of an item as the feed showed it (the caller reads it from the cache the screen renders
 * from and throws when it holds none). A feed write names it, so a colleague's change in between is
 * refused (412) and never overwritten; no write reads a fresh version.
 */
export type SeenVersion = (itemId: string) => number;

/** What a status or assignee write answers: the item's new version. */
interface VersionAnswer {
  readonly version?: number | null;
}

/** The version a chained write names: the one the server returned for the write before it. */
function nextVersion(answer: VersionAnswer | null | undefined): number {
  if (typeof answer?.version !== "number")
    throw new Error("The server sent no version for the item after a write.");
  return answer.version;
}

function readBoard(api: FeedApi, boardId: string): Promise<FullBoardLike> {
  return api.get<FullBoardLike>(`/boards/${boardId}/full`, QUIET);
}

/** The Status option of the Board that sits in the bucket (the first one, in board order). */
function statusOptionOf(
  board: FullBoardLike,
  bucket: WorkBucket,
): string | null {
  const field = (board.columns ?? []).find(
    (column) =>
      !column.isDeleted && (column.fieldType ?? "").toLowerCase() === "status",
  );
  const option = (field?.options ?? []).find(
    (candidate) =>
      candidate.id && bucketOfCategory(candidate.category) === bucket,
  );
  return option?.id ?? null;
}

async function setStatusOnBoard(
  api: FeedApi,
  itemId: string,
  board: FullBoardLike,
  bucket: WorkBucket,
  expectedVersion: number,
): Promise<"ok" | "invalid"> {
  const optionId = statusOptionOf(board, bucket);
  if (!optionId) return "invalid";
  await api.patch<VersionAnswer | null>(
    `/board-items/${itemId}/status`,
    { status: optionId, expectedVersion },
    { idempotencyKey: newKey(), ...QUIET },
  );
  return "ok";
}

/** Sets an item's status to the Board's option for the bucket. */
export async function setItemBucket(
  api: FeedApi,
  input: {
    itemId: string;
    boardId: string;
    bucket: WorkBucket;
    versionOf: SeenVersion;
  },
): Promise<void> {
  const expectedVersion = input.versionOf(input.itemId);
  const board = await readBoard(api, input.boardId);
  const outcome = await setStatusOnBoard(
    api,
    input.itemId,
    board,
    input.bucket,
    expectedVersion,
  );
  if (outcome === "invalid") {
    throw new Error("The board has no status for that step.");
  }
}

/**
 * Creates an item in the Board's first open group, assigns the member and
 * sets it to To do: the three producer writes behind "assigned to you, set
 * to To do".
 */
export async function createAssignedItem(
  api: FeedApi,
  input: {
    boardId: string;
    name: string;
    userId: string;
    /** The item's description, written with the create. */
    descriptionMd?: string;
    /** `YYYY-MM-DD`: the due day, written with the create. */
    dueDay?: string;
  },
): Promise<{ id: string; key: string | null }> {
  const board = await readBoard(api, input.boardId);
  const group = (board.lists ?? []).find(
    (candidate) => candidate.id && !candidate.isArchived,
  );
  if (!group?.id) throw new Error("The board has no group to put the item in.");

  const created = await api.post<{ id: string; version?: number | null }>(
    `/boards/${input.boardId}/items`,
    {
      groupId: group.id,
      title: input.name,
      previousSiblingId: null,
      nextSiblingId: null,
      ...(input.descriptionMd?.trim()
        ? { descriptionMd: input.descriptionMd }
        : {}),
      ...(input.dueDay ? { dueDate: `${input.dueDay}T00:00:00.000Z` } : {}),
    },
    { idempotencyKey: newKey(), ...QUIET },
  );
  // The create answers the item's first version; the assign answers the one the status write names.
  const assigned = await api.post<VersionAnswer | null>(
    `/board-items/${created.id}/assignees`,
    { userId: input.userId, expectedVersion: nextVersion(created) },
    { idempotencyKey: newKey(), ...QUIET },
  );
  await setStatusOnBoard(api, created.id, board, "todo", nextVersion(assigned));
  return { id: created.id, key: null };
}

/**
 * Applies one action to many items, one producer write per item. A failed
 * item is reported in the result and never stops the others.
 */
export async function bulkEach(
  api: FeedApi,
  input: {
    itemIds: readonly string[];
    boardIdOf: (itemId: string) => string | null;
    action: BulkAction;
    versionOf: SeenVersion;
  },
): Promise<BulkResult> {
  const { action } = input;
  const boards = new Map<string, Promise<FullBoardLike>>();
  const boardOf = (boardId: string) => {
    const known = boards.get(boardId);
    if (known) return known;
    const read = readBoard(api, boardId);
    boards.set(boardId, read);
    return read;
  };

  const apply = async (itemId: string): Promise<"ok" | "invalid"> => {
    switch (action.type) {
      case "unfollow":
        await api.delete<void>(`/board-items/${itemId}/following`, {
          idempotencyKey: newKey(),
          ...QUIET,
        });
        return "ok";
      case "set-due":
        await api.patch<void>(
          `/board-items/${itemId}/due-date`,
          {
            dueDate: action.dueAt,
            startDate: null,
            expectedVersion: input.versionOf(itemId),
          },
          { idempotencyKey: newKey(), ...QUIET },
        );
        return "ok";
      case "assign":
        await api.post<void>(
          `/board-items/${itemId}/assignees`,
          {
            userId: action.assigneeId,
            expectedVersion: input.versionOf(itemId),
          },
          { idempotencyKey: newKey(), ...QUIET },
        );
        return "ok";
      case "set-status": {
        const boardId = input.boardIdOf(itemId);
        if (!boardId) return "invalid";
        return setStatusOnBoard(
          api,
          itemId,
          await boardOf(boardId),
          action.bucket,
          input.versionOf(itemId),
        );
      }
    }
  };

  return {
    results: await Promise.all(
      input.itemIds.map(async (itemId) => {
        try {
          const outcome = await apply(itemId);
          return outcome === "ok"
            ? ({ itemId, ok: true, error: null } as const)
            : ({ itemId, ok: false, error: "invalid" } as const);
        } catch {
          return { itemId, ok: false, error: "invalid" } as const;
        }
      }),
    ),
  };
}
