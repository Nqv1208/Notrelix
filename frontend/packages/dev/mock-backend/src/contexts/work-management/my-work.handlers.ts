/**
 * Work Management — My work read and Item completion (WX-BE-005,
 * WX-HOME-001). Shapes mirror the producer: `CursorPage<MyWorkItemDto>` and
 * an idempotent `POST /board-items/:id/complete`.
 */
import { defineMockOperation } from "../../operations/types";
import { rollRepeatOnCompletion } from "./item-capabilities.handlers";
import {
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";

const DEFAULT_LIMIT = 50;
const MAX_LIMIT = 100;

export const myWorkOperations = [
  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "wm.myWork.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.ListMyWork",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/my-work/items",
    async handle({ params, store, request }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const requested = Number(
        request.searchParams.get("limit") ?? DEFAULT_LIMIT,
      );
      const limit = Number.isFinite(requested)
        ? Math.min(MAX_LIMIT, Math.max(1, Math.floor(requested)))
        : DEFAULT_LIMIT;
      const cursor = request.searchParams.get("cursor");
      const items = store.getMyWorkItems(params.workspaceId);
      const start = cursor ? Math.max(0, Number(cursor) || 0) : 0;
      const page = items.slice(start, start + limit);
      const next = start + page.length;
      return ok({
        items: page,
        nextCursor: next < items.length ? String(next) : null,
        hasNextPage: next < items.length,
      });
    },
  }),

  defineMockOperation<{ id: string }, { completedAt?: string | null }, void>({
    id: "wm.boardItems.complete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Complete",
    },
    method: "POST",
    route: "/board-items/:id/complete",
    async handle({ params, store, request }) {
      if (!request.headers.get("Idempotency-Key")) {
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      }
      const version = () => ({ version: store.getCard(params.id)?.version ?? 1 });
      if (rollRepeatOnCompletion(store, params.id)) return ok(version());
      return store.completeCard(params.id) ? ok(version()) : notFound();
    },
  }),
];
