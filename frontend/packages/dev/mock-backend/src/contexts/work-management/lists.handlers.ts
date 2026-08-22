/**
 * Work Management — BoardGroups context handlers (formerly Lists).
 */

import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";

export const listsOperations = [
  // ─── POST /boards/:boardId/groups ─────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { title?: string; color?: string; position?: number },
    void
  >({
    id: "wm.boardGroups.create",
    contract: { kind: "openapi", operationId: "WorkManagement.BoardGroups.Create" } as any,
    method: "POST",
    route: "/boards/:boardId/groups",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        color?: string;
        position?: number;
      };
      store.createList(params.boardId, {
        title: data.title,
        color: data.color,
        position: data.position,
      });
      return ok<void>(undefined);
    },
  }),

  // ─── PATCH /board-groups/:groupId ────────────────────────────────────────────────

  defineMockOperation<
    { groupId: string },
    { title?: string; color?: string; isArchived?: boolean },
    void
  >({
    id: "wm.boardGroups.update",
    contract: { kind: "openapi", operationId: "WorkManagement.BoardGroups.Update" } as any,
    method: "PATCH",
    route: "/board-groups/:groupId",
    async handle({ params, body, store }) {
      const updated = store.updateList(params.groupId, body ?? {});
      if (!updated) return notFound("Group not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /board-groups/:groupId ───────────────────────────────────────────────

  defineMockOperation<{ groupId: string }, never, void>({
    id: "wm.boardGroups.delete",
    contract: { kind: "openapi", operationId: "WorkManagement.BoardGroups.Delete" } as any,
    method: "DELETE",
    route: "/board-groups/:groupId",
    async handle({ params, store }) {
      const deleted = store.deleteList(params.groupId);
      if (!deleted) return notFound("Group not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /board-groups/:groupId/duplicate ───────────────────────────────────────

  defineMockOperation<{ groupId: string }, never, void>({
    id: "wm.boardGroups.duplicate",
    contract: { kind: "openapi", operationId: "WorkManagement.BoardGroups.Duplicate" } as any,
    method: "POST",
    route: "/board-groups/:groupId/duplicate",
    async handle({ params, store }) {
      const duplicated = store.duplicateList(params.groupId);
      if (!duplicated) return notFound("Group not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /boards/:boardId/groups/reorder ─────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { items?: { id: string; newPosition: number }[] },
    void
  >({
    id: "wm.boardGroups.reorder",
    contract: { kind: "openapi", operationId: "WorkManagement.BoardGroups.Reorder" } as any,
    method: "POST",
    route: "/boards/:boardId/groups/reorder",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        items?: { id: string; newPosition: number }[];
      };
      if (data.items) {
        store.reorderLists(params.boardId, data.items);
      }
      return ok<void>(undefined);
    },
  }),
];
