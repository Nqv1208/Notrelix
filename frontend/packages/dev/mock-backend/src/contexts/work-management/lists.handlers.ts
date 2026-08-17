/**
 * Work Management — Lists context handlers.
 *
 * Operations:
 *   lists.byBoard  — GET /boards/:boardId/lists
 *   lists.create   — POST /boards/:boardId/lists
 *   lists.reorder  — POST /boards/:boardId/lists/reorder
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import type { ListDtoApi } from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import { ok, created } from "../../transport/create-response";

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

  defineMockOperation<{ boardId: string }, { title?: string }, ListDtoApi>({
    id: "lists.create",
    method: "POST",
    route: "/boards/:boardId/lists",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as { title?: string };
      const factories = store.getFactories();
      const newList = factories.list(store.getLists(params.boardId).length, params.boardId, {
        id: `list-new-${Date.now()}`,
        title: data.title ?? "New List",
      });
      store.addList(newList);
      return created<ListDtoApi>({
        id: newList.id,
        title: newList.title,
        color: newList.color ?? null,
        position: newList.position,
        isArchived: false,
        cards: [],
      });
    },
  }),

  // ─── POST /boards/:boardId/lists/reorder ─────────────────────────────────

  defineMockOperation<{ boardId: string }, { order?: string[] }>({
    id: "lists.reorder",
    method: "POST",
    route: "/boards/:boardId/lists/reorder",
    async handle({ params, store }) {
      return ok({ boardId: params.boardId, count: store.getLists(params.boardId).length });
    },
  }),
];
