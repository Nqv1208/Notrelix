/**
 * Work Management — Lists context handlers.
 *
 * Operations:
 *   lists.byBoard   — GET /boards/:boardId/lists
 *   lists.create    — POST /boards/:boardId/lists (returns string listId)
 *   lists.update    — PATCH /lists/:listId
 *   lists.delete    — DELETE /lists/:listId
 *   lists.duplicate — POST /lists/:listId/duplicate (returns string listId)
 *   lists.reorder   — POST /boards/:boardId/lists/reorder
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import type { ListDtoApi } from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";

export const listsOperations = [
  // ─── GET /boards/:boardId/lists ───────────────────────────────────────────

  defineMockOperation<{ boardId: string }, never, ListDtoApi[]>({
    id: "lists.byBoard",
    method: "GET",
    route: "/boards/:boardId/lists",
    async handle({ params, store }) {
      const lists = store.getLists(params.boardId);
      return ok<ListDtoApi[]>(
        lists.map((l) => ({
          id: l.id,
          title: l.title,
          color: l.color ?? null,
          position: l.position,
          isArchived: false,
          cards: store.getCards(l.id).map((c) => ({
            id: c.id,
            title: c.title,
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
            position: c.position,
          })),
        })),
      );
    },
  }),

  // ─── POST /boards/:boardId/lists ─────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { title?: string; color?: string; position?: number },
    string
  >({
    id: "lists.create",
    method: "POST",
    route: "/boards/:boardId/lists",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        color?: string;
        position?: number;
      };
      const newList = store.createList(params.boardId, {
        title: data.title,
        color: data.color,
        position: data.position,
      });
      return created<string>(newList.id);
    },
  }),

  // ─── PATCH /lists/:listId ────────────────────────────────────────────────

  defineMockOperation<
    { listId: string },
    { title?: string; color?: string; isArchived?: boolean },
    void
  >({
    id: "lists.update",
    method: "PATCH",
    route: "/lists/:listId",
    async handle({ params, body, store }) {
      const updated = store.updateList(params.listId, body ?? {});
      if (!updated) return notFound("List not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /lists/:listId ───────────────────────────────────────────────

  defineMockOperation<{ listId: string }, never, void>({
    id: "lists.delete",
    method: "DELETE",
    route: "/lists/:listId",
    async handle({ params, store }) {
      const deleted = store.deleteList(params.listId);
      if (!deleted) return notFound("List not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /lists/:listId/duplicate ───────────────────────────────────────

  defineMockOperation<{ listId: string }, never, string>({
    id: "lists.duplicate",
    method: "POST",
    route: "/lists/:listId/duplicate",
    async handle({ params, store }) {
      const duplicated = store.duplicateList(params.listId);
      if (!duplicated) return notFound("List not found");
      return created<string>(duplicated.id);
    },
  }),

  // ─── POST /boards/:boardId/lists/reorder ─────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { items?: { id: string; newPosition: number }[] },
    void
  >({
    id: "lists.reorder",
    method: "POST",
    route: "/boards/:boardId/lists/reorder",
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
