/**
 * Work Management — Boards context handlers.
 *
 * Operations:
 *   boards.listByWorkspace — GET /workspaces/:workspaceId/boards
 *   boards.detail          — GET /boards/:id
 *   boards.full            — GET /boards/:id/full
 *   boards.create          — POST /workspaces/:workspaceId/boards
 *   boards.view.get        — GET /boards/:id/view
 *   boards.view.save       — PUT /boards/:id/view
 *   boards.columns.list    — GET /boards/:boardId/fields
 *   boards.columns.create  — POST /boards/:boardId/fields
 *   boards.columns.update  — PATCH /boards/:boardId/fields/:columnId
 *   boards.columns.delete  — DELETE /boards/:boardId/fields/:columnId
 *   boards.columns.move — POST /boards/:boardId/fields/:fieldId/move
 *   boards.labels.list     — GET /boards/:boardId/labels
 *   boards.labels.create   — POST /boards/:boardId/labels
 *   boards.labels.update   — PATCH /boards/:boardId/labels/:labelId
 *   boards.labels.delete   — DELETE /boards/:boardId/labels/:labelId
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import type {
  BoardDtoApi,
  FullBoardDtoApi,
  CardSummaryDtoApi,
  ListDtoApi,
  BoardViewDtoApi,
} from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import {
  ok,
  created,
  forbidden,
  noContent,
  notFound,
  validationError,
} from "../../transport/create-response";
import { accessAtLeast } from "../../state/mock-spaces";
import type { MockStore } from "../../state/mock-store";

/** The Board's Space id, masked when the caller cannot see it (GOV-029). */
function visibleSpaceId(
  store: MockStore,
  workspaceId: string,
  spaceId: string | null | undefined,
): string | null {
  if (!spaceId) return null;
  return store.spaces.accessFor(spaceId, store.spaceViewer(workspaceId)) ===
    null
    ? null
    : spaceId;
}

/** GOV-028: creating or moving a Board into a Space needs Editor there. */
function targetSpaceDenied(
  store: MockStore,
  workspaceId: string,
  spaceId: string | null,
) {
  if (!spaceId) return null;
  const space = store.spaces.getSpace(spaceId);
  const access = space
    ? store.spaces.accessFor(spaceId, store.spaceViewer(workspaceId))
    : null;
  if (!space || space.workspaceId !== workspaceId || access === null)
    return notFound();
  if (!accessAtLeast(access, "Editor")) return forbidden();
  if (space.archived) {
    return {
      status: 409,
      body: {
        type: "https://docs.notrelix.com/problems/workspaces-space-inactive",
        title: "Conflict",
        status: 409,
        detail: "The target space is archived.",
        errorCode: "workspaces.space.inactive",
      },
    };
  }
  return null;
}

function projectCardSummary(
  card: { id: string; title: string; position: number; createdAt: string },
  _listId: string,
): CardSummaryDtoApi {
  return {
    id: card.id,
    title: card.title,
    priority: null,
    status: "todo",
    dueDate: null,
    cover: null,
    memberCount: 0,
    members: [],
    labels: [],
    checklistProgress: 0,
    checklistTotal: 0,
    commentCount: 0,
    attachmentCount: 0,
    position: card.position,
  };
}

export const boardsOperations = [
  // ─── GET /workspaces/:workspaceId/boards ──────────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, BoardDtoApi[]>({
    id: "boards.listByWorkspace",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces/:workspaceId/boards",
    async handle({ params, store }) {
      const boards = store.getBoards(params.workspaceId);
      return ok<BoardDtoApi[]>(
        boards.map((b) => ({
          id: b.id,
          workspaceId: b.workspaceId,
          title: b.title,
          description: b.description ?? null,
          background: JSON.stringify(b.background),
          visibility: b.visibility,
          isArchived: b.isArchived,
          memberCount: 1,
          listCount: store.getLists(b.id).length,
          createdAt: b.createdAt,
          version: b.version ?? 1,
          spaceId: visibleSpaceId(store, params.workspaceId, b.spaceId),
        })),
      );
    },
  }),

  // ─── GET /boards/:id ─────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BoardDtoApi>({
    id: "boards.detail",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/boards/:id",
    async handle({ params, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      return ok<BoardDtoApi>({
        id: b.id,
        workspaceId: b.workspaceId,
        title: b.title,
        description: b.description ?? null,
        background: JSON.stringify(b.background),
        visibility: b.visibility,
        isArchived: b.isArchived,
        memberCount: 1,
        listCount: store.getLists(b.id).length,
        createdAt: b.createdAt,
      });
    },
  }),

  // ─── GET /boards/:id/full ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, FullBoardDtoApi>({
    id: "boards.full",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/boards/:id/full",
    async handle({ params, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");

      const lists: ListDtoApi[] = store.getLists(b.id).map((l) => ({
        id: l.id,
        title: l.title,
        color: l.color ?? null,
        position: l.position,
        isArchived: false,
        cards: store.getCards(l.id).map((c) => projectCardSummary(c, l.id)),
      }));

      return ok<FullBoardDtoApi>({
        id: b.id,
        title: b.title,
        description: b.description ?? null,
        background: JSON.stringify(b.background),
        visibility: b.visibility,
        lists,
        members: [],
      });
    },
  }),

  // ─── POST /workspaces/:workspaceId/boards ─────────────────────────────────

  defineMockOperation<
    { workspaceId: string },
    { title?: string; description?: string },
    string
  >({
    id: "boards.create",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/workspaces/:workspaceId/boards",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        description?: string;
        spaceId?: string | null;
      };
      const denied = targetSpaceDenied(
        store,
        params.workspaceId,
        data.spaceId ?? null,
      );
      if (denied) return denied;
      const newBoard = store.createBoard(params.workspaceId, {
        title: data.title,
        description: data.description,
        spaceId: data.spaceId ?? null,
      });
      // Like the producer (`ToCreatedResult`): 201 with the new Board id.
      return created<string>(newBoard.id);
    },
  }),

  // ─── POST /boards/:boardId/move-to-space (GOV-028) ─────────────────────────

  defineMockOperation<
    { boardId: string },
    { spaceId?: string | null; expectedVersion?: number },
    void
  >({
    id: "boards.moveToSpace",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.MoveToSpace",
    },
    method: "POST",
    route: "/boards/:boardId/move-to-space",
    async handle({ params, body, store, request }) {
      if (!request.headers.get("Idempotency-Key")) {
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      }
      const board = store.getBoard(params.boardId);
      if (!board || !store.isCurrentMemberOf(board.workspaceId))
        return notFound();
      const role = store.getCurrentMemberRole(board.workspaceId);
      // ManageBoard on the source (guests never manage Boards here).
      if (role === "guest") return forbidden();
      const target = body?.spaceId ?? null;
      const denied = targetSpaceDenied(store, board.workspaceId, target);
      if (denied) return denied;
      switch (store.placeBoard(board.id, target, body?.expectedVersion)) {
        case "ok":
          return noContent();
        case "not-found":
          return notFound();
        case "stale":
          return {
            status: 412,
            body: {
              type: "https://docs.notrelix.com/problems/common-precondition-failed",
              title: "Precondition failed",
              status: 412,
              detail:
                "The resource was modified by another request. Reload and retry.",
              errorCode: "common.precondition-failed",
            },
          };
      }
    },
  }),

  // ─── GET /boards/:id/view ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BoardViewDtoApi>({
    id: "boards.view.get",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Get",
    } as any,
    method: "GET",
    route: "/boards/:id/views",
    async handle({ params, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      const v = store.getBoardView(params.id);
      return ok<BoardViewDtoApi>({
        viewMode: v?.viewMode ?? "table",
        config: v?.viewConfig ?? "{}",
        filters: v?.filters ?? "{}",
      });
    },
  }),

  // ─── PUT /boards/:id/view ─────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { viewMode?: string; config?: string; filters?: string },
    void
  >({
    id: "boards.view.save",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Save",
    } as any,
    method: "PUT",
    route: "/boards/:id/views",
    async handle({ params, body, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      const data = (body ?? {}) as {
        viewMode?: string;
        config?: string;
        filters?: string;
      };
      store.saveBoardView(
        params.id,
        data.viewMode ?? "table",
        data.config ?? "{}",
        data.filters,
      );
      return ok<void>(undefined);
    },
  }),

  // ─── GET /boards/:boardId/fields ─────────────────────────────────────────

  defineMockOperation<{ boardId: string }, never, unknown[]>({
    id: "boards.columns.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/boards/:boardId/fields",
    async handle({ params, store }) {
      const cols = store.getColumns(params.boardId);
      return ok(
        cols.map((c) => ({
          id: c.id,
          boardId: c.boardId,
          name: c.name,
          fieldType: c.fieldType,
          settings: c.settings ? JSON.parse(c.settings) : undefined,
          position: c.position,
          isHidden: c.isHidden ?? false,
        })),
      );
    },
  }),

  // ─── POST /boards/:boardId/fields ────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { name: string; fieldType: string; settings?: string; position?: number },
    string
  >({
    id: "boards.columns.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Create",
    } as any,
    method: "POST",
    route: "/boards/:boardId/fields",
    async handle({ params, body, store }) {
      const data = body as {
        name: string;
        fieldType: string;
        settings?: string;
        position?: number;
      };
      const col = store.createColumn(params.boardId, {
        name: data.name,
        fieldType: data.fieldType,
        settings: data.settings,
        position: data.position,
      });
      return created<string>(col.id);
    },
  }),

  // ─── PATCH /boards/:boardId/fields/:columnId ─────────────────────────────

  defineMockOperation<
    { boardId: string; columnId: string },
    {
      name?: string;
      fieldType?: string;
      settings?: string;
      isHidden?: boolean;
    },
    void
  >({
    id: "boards.columns.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Update",
    } as any,
    method: "PATCH",
    route: "/boards/:boardId/fields/:columnId",
    async handle({ params, body, store }) {
      const updated = store.updateColumn(params.columnId, body ?? {});
      if (!updated) return notFound("Column not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /boards/:boardId/fields/:columnId ────────────────────────────

  defineMockOperation<{ boardId: string; columnId: string }, never, void>({
    id: "boards.columns.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Delete",
    } as any,
    method: "DELETE",
    route: "/boards/:boardId/fields/:columnId",
    async handle({ params, store }) {
      const deleted = store.deleteColumn(params.columnId);
      if (!deleted) return notFound("Column not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /boards/:boardId/fields/:fieldId/move ─────────────────────────

  defineMockOperation<
    { boardId: string; fieldId: string },
    { previousSiblingId?: string | null; nextSiblingId?: string | null },
    void
  >({
    id: "boards.columns.move",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Move",
    } as any,
    method: "POST",
    route: "/boards/:boardId/fields/:fieldId/move",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        previousSiblingId?: string | null;
        nextSiblingId?: string | null;
      };
      const moved = store.moveColumn(
        params.boardId,
        params.fieldId,
        data.previousSiblingId,
        data.nextSiblingId,
      );
      if (!moved) return notFound("Column not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /boards/:boardId/labels ──────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    never,
    { id: string; name: string; color: string }[]
  >({
    id: "boards.labels.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.List",
    } as any,
    method: "GET",
    route: "/boards/:boardId/labels",
    async handle({ params, store }) {
      const labels = store.getBoardLabels(params.boardId);
      return ok(
        labels.map((l) => ({
          id: l.id,
          name: l.name,
          color: l.color,
        })),
      );
    },
  }),

  // ─── POST /boards/:boardId/labels ─────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { name?: string; color: string },
    { id: string; name: string; color: string }
  >({
    id: "boards.labels.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.Create",
    } as any,
    method: "POST",
    route: "/boards/:boardId/labels",
    async handle({ params, body, store }) {
      const data = (body ?? { color: "#1E90FF" }) as {
        name?: string;
        color: string;
      };
      const label = store.createLabel(params.boardId, data);
      return created<{ id: string; name: string; color: string }>({
        id: label.id,
        name: label.name,
        color: label.color,
      });
    },
  }),

  // ─── PATCH /boards/:boardId/labels/:labelId ───────────────────────────────

  defineMockOperation<
    { boardId: string; labelId: string },
    { name?: string; color?: string },
    void
  >({
    id: "boards.labels.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.Update",
    } as any,
    method: "PATCH",
    route: "/boards/:boardId/labels/:labelId",
    async handle({ params, body, store }) {
      const updated = store.updateLabel(params.labelId, body ?? {});
      if (!updated) return notFound("Label not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /boards/:boardId/labels/:labelId ──────────────────────────────

  defineMockOperation<{ boardId: string; labelId: string }, never, void>({
    id: "boards.labels.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.Delete",
    } as any,
    method: "DELETE",
    route: "/boards/:boardId/labels/:labelId",
    async handle({ params, store }) {
      const deleted = store.deleteLabel(params.labelId);
      if (!deleted) return notFound("Label not found");
      return ok<void>(undefined);
    },
  }),
];
