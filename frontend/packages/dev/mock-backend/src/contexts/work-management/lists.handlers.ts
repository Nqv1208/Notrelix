/**
 * Work Management — BoardGroups context handlers (formerly Lists).
 */

import { defineMockOperation } from "../../operations/types";
import { notFound, ok, validationError } from "../../transport/create-response";

export const listsOperations = [
  // ─── POST /boards/:boardId/groups ─────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { title?: string; color?: string; position?: number },
    void
  >({
    id: "wm.boardGroups.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardGroups.Create",
    } as any,
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
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardGroups.Update",
    } as any,
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
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardGroups.Delete",
    } as any,
    method: "DELETE",
    route: "/board-groups/:groupId",
    async handle({ params, store, query }) {
      const list = store.getList(params.groupId);
      if (!list) return notFound("Group not found");
      // The producer keeps a board's last group and moves the items of a deleted
      // group to a target group of the same board.
      if (store.getLists(list.boardId).length <= 1)
        return validationError({
          groupId: ["The last group of a board cannot be deleted."],
        });
      const cards = store.getCards(params.groupId);
      if (cards.length > 0) {
        const target = query.targetGroupId
          ? store.getList(query.targetGroupId)
          : undefined;
        if (!query.targetGroupId)
          return validationError({
            targetGroupId: [
              "A group that has items can only be deleted together with a target group for them.",
            ],
          });
        if (!target || target.id === list.id || target.boardId !== list.boardId)
          return validationError({
            targetGroupId: [
              "The target group must be another group of the same board.",
            ],
          });
        const last = store
          .getCards(target.id)
          .reduce((max, card) => Math.max(max, card.position), 0);
        cards.forEach((card, index) =>
          store.moveCard(card.id, target.id, last + index + 1),
        );
      }
      store.deleteList(params.groupId);
      return ok<void>(undefined);
    },
  }),

  // ─── POST /board-groups/:groupId/duplicate ───────────────────────────────────────

  defineMockOperation<{ groupId: string }, never, void>({
    id: "wm.boardGroups.duplicate",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardGroups.Duplicate",
    } as any,
    method: "POST",
    route: "/board-groups/:groupId/duplicate",
    async handle({ params, store }) {
      const duplicated = store.duplicateList(params.groupId);
      if (!duplicated) return notFound("Group not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /board-groups/:groupId/move ──────────────────────────────────────

  defineMockOperation<
    { groupId: string },
    { previousSiblingId?: string | null; nextSiblingId?: string | null },
    void
  >({
    id: "wm.boardGroups.move",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardGroups.Move",
    } as any,
    method: "POST",
    route: "/board-groups/:groupId/move",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        previousSiblingId?: string | null;
        nextSiblingId?: string | null;
      };
      const moved = store.moveList(
        params.groupId,
        data.previousSiblingId,
        data.nextSiblingId,
      );
      if (!moved) return notFound("Group not found");
      return ok<void>(undefined);
    },
  }),
];
