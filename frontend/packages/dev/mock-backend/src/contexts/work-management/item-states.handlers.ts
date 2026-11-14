/**
 * Work Management — what the States mockup adds around an item (gap
 * `CTR-GAP-WM-ITEM-STATES`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: States"):
 *
 *   itemStates.lifecycle    — GET  /board-items/:id/lifecycle
 *   itemStates.restore      — POST /board-items/:id/unarchive
 */
import { defineMockOperation } from "../../operations/types";
import {
  forbidden,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import { canReadBoard, canWriteBoardItems } from "./boards.handlers";

interface ItemLifecycleDto {
  readonly archived: { readonly at: string; readonly byName: string } | null;
  readonly completed: { readonly at: string; readonly byName: string } | null;
}

const gap = { kind: "gap", gapId: "CTR-GAP-WM-ITEM-STATES" } as const;

export const itemStatesOperations = [
  defineMockOperation<{ id: string }, never, ItemLifecycleDto>({
    id: "itemStates.lifecycle",
    contract: gap,
    method: "GET",
    route: "/board-items/:id/lifecycle",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store)) return notFound();
      const nameOf = (userId: string | null | undefined) =>
        (userId ? store.getUser(userId)?.name : null) ?? "";
      return ok<ItemLifecycleDto>({
        archived:
          item.isArchived && item.archivedAt
            ? {
                at: item.archivedAt,
                byName: nameOf(item.archivedByUserId),
              }
            : null,
        completed: item.completedAt
          ? {
              at: item.completedAt,
              byName: nameOf(item.assigneeUserIds?.[0]),
            }
          : null,
      });
    },
  }),

  defineMockOperation<{ boardId: string }, never, unknown>({
    id: "wm.boardItems.archived",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Archived",
    },
    method: "GET",
    route: "/boards/:boardId/items/archived",
    async handle({ params, store, request }) {
      if (!canWriteBoardItems(params.boardId, store)) return forbidden();
      const url = new URL(request.url);
      const term = (url.searchParams.get("q") ?? "").trim().toLowerCase();
      const all = store
        .getArchivedCards(params.boardId)
        .filter(
          (card) =>
            !term ||
            card.title.toLowerCase().includes(term) ||
            (card.itemKey ?? "").toLowerCase().includes(term),
        );
      const start = Number(url.searchParams.get("cursor") ?? 0) || 0;
      return ok({
        items: all.slice(start, start + 50).map((card) => ({
          id: card.id,
          name: card.title,
          itemKey: card.itemKey ?? null,
          groupId: card.listId,
          archivedAt: card.archivedAt ?? "",
          archivedBy: card.archivedByUserId ?? null,
          archivedByName: card.archivedByUserId
            ? (store.getUser(card.archivedByUserId)?.name ?? null)
            : null,
          subItemCount: store.getChildCards(card.id).length,
          version: card.version ?? 1,
        })),
        nextCursor: start + 50 < all.length ? String(start + 50) : null,
        total: all.length,
      });
    },
  }),

  defineMockOperation<{ id: string }, never, void>({
    id: "itemStates.restore",
    contract: gap,
    method: "POST",
    route: "/board-items/:id/unarchive",
    async handle({ params, store, request }) {
      if (!request.headers.get("Idempotency-Key"))
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      const item = store.getCard(params.id);
      if (!item) return notFound();
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      store.restoreCard(params.id);
      return ok({ version: store.getCard(params.id)?.version ?? 1 });
    },
  }),
];
