import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";

/**
 * Structural types (no `@notrelix/realtime` import: the state package does not
 * depend on the realtime foundation). They match `RealtimeEnvelope` and
 * `ModuleRealtimeContext` of the foundation.
 */
export interface WorkManagementRealtimeEnvelope {
  readonly eventType: string;
  readonly resourceKind: string;
  readonly resourceId: string;
  readonly workspaceId: string;
}

export interface WorkManagementRealtimeContext {
  readonly workspaceId: string;
  /** The board the frame's subscription belongs to; absent on a workspace subscription. */
  readonly boardId?: string;
  readonly invalidateQueries: (
    keys: readonly unknown[][],
  ) => Promise<void> | void;
}

/** The Board topic the backend announces every Board change on (`board.board`). */
const BOARD_EVENT_TYPE = "board.board";

/** Item-keyed query families (their keys carry the item id, not the board). */
const ITEM_FAMILIES = [
  "cards",
  "item-children",
  "item-following",
  "item-approvals",
  "item-links",
  "item-linked-pages",
  "item-votes",
  "item-lifecycle",
  "plan-items",
] as const;

/**
 * The queries one board change can make stale: the board's own reads and the
 * item families of the workspace. Other boards and workspace-level reads
 * (My work, board lists, templates) are not touched.
 */
export function boardRealtimeQueryKeys(
  workspaceId: string,
  boardId: string,
): unknown[][] {
  return [
    wmQueryKeys.fullBoard(workspaceId, boardId),
    wmQueryKeys.view(workspaceId, boardId),
    wmQueryKeys.groups(workspaceId, boardId),
    wmQueryKeys.columns(workspaceId, boardId),
    wmQueryKeys.boardLabels(workspaceId, boardId),
    wmQueryKeys.boardChecklists(workspaceId, boardId),
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "settings",
      boardId,
    ),
    wmQueryKeys.boardSavedFilters(workspaceId, boardId),
    wmQueryKeys.boardAccess(workspaceId, boardId),
    wmQueryKeys.boardTrash(workspaceId, boardId),
    ...ITEM_FAMILIES.map((family) =>
      workspaceQueryKey(workspaceId, "work-management", family),
    ),
  ].map((key) => [...key]);
}

/**
 * Work Management realtime adapter (WM-032).
 *
 * A frame is notify-only: it says something on one Board changed, not what.
 * The adapter therefore never patches a cache; it marks the open board's queries stale and the mounted ones (board read, item detail,
 * comments, checklists, labels, saved filters, ...) refetch over REST, where
 * authorization is evaluated again.
 *
 * Ordering is the transport's job: it drops duplicate event ids and turns a
 * missing, repeated or reset stream version into a recovery signal, which the
 * host answers with a workspace-wide refetch. An invalidation is idempotent,
 * so a duplicate or out-of-order frame that reaches the adapter costs one
 * extra refetch and can never overwrite newer state.
 */
export const workManagementRealtimeAdapter = {
  id: "work-management-adapter",
  supports(envelope: WorkManagementRealtimeEnvelope): boolean {
    return envelope.eventType === BOARD_EVENT_TYPE;
  },
  async validateAndHandle(
    envelope: WorkManagementRealtimeEnvelope,
    context: WorkManagementRealtimeContext,
  ): Promise<void> {
    // A frame of another workspace or another board than the open one belongs
    // to a previous scope: it must not touch the current one (NRX-014).
    if (envelope.workspaceId !== context.workspaceId) return;
    if (!context.boardId || envelope.resourceId !== context.boardId) return;

    await context.invalidateQueries(
      boardRealtimeQueryKeys(context.workspaceId, context.boardId),
    );
  },
};
