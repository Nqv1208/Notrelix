/**
 * Work Management — Cards context handlers.
 *
 * Operations:
 *   cards.byList  — GET /lists/:listId/cards
 *   cards.detail  — GET /cards/:id
 *   cards.create  — POST /lists/:listId/cards
 *   cards.move    — POST /cards/:id/move
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 * §Stateful mutation rule: mutations modify MockStore queried by later reads.
 */

import type { CardDtoApi } from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";

function projectCardDto(
  c: {
    id: string;
    boardId: string;
    listId: string;
    title: string;
    description?: string;
    position: number;
    createdAt: string;
    updatedAt: string;
  },
  workspaceId: string,
): CardDtoApi {
  return {
    id: c.id,
    boardId: c.boardId,
    workspaceId,
    listId: c.listId,
    title: c.title,
    descriptionMd: c.description ?? null,
    linkedPageId: null,
    priority: null,
    status: "todo",
    dueDate: null,
    startDate: null,
    completedAt: null,
    cover: null,
    position: c.position,
    members: [],
    labels: [],
    checklists: [],
    commentCount: 0,
    attachmentCount: 0,
    fieldValues: null,
    createdAt: c.createdAt,
    updatedAt: c.updatedAt ?? null,
  };
}

export const cardsOperations = [
  // ─── GET /lists/:listId/cards ─────────────────────────────────────────────

  defineMockOperation<{ listId: string }, never, CardDtoApi[]>({
    id: "cards.byList",
    method: "GET",
    route: "/lists/:listId/cards",
    async handle({ params, store }) {
      const cards = store.getCards(params.listId);
      return ok<CardDtoApi[]>(
        cards.map((c) => {
          const board = store.getBoard(c.boardId);
          return projectCardDto(c, board?.workspaceId ?? "");
        }),
      );
    },
  }),

  // ─── GET /cards/:id ───────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, CardDtoApi>({
    id: "cards.detail",
    method: "GET",
    route: "/cards/:id",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      const board = store.getBoard(c.boardId);
      return ok<CardDtoApi>(projectCardDto(c, board?.workspaceId ?? ""));
    },
  }),

  // ─── POST /lists/:listId/cards ────────────────────────────────────────────

  defineMockOperation<
    { listId: string },
    { title?: string; boardId?: string },
    CardDtoApi
  >({
    id: "cards.create",
    method: "POST",
    route: "/lists/:listId/cards",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as { title?: string; boardId?: string };
      if (!data.boardId) {
        return {
          status: 400,
          body: { message: "boardId required", code: "BAD_REQUEST" },
        };
      }
      const newCard = store.createCard(data.boardId, params.listId, {
        title: data.title,
      });
      const board = store.getBoard(data.boardId);
      return created<CardDtoApi>(
        projectCardDto(newCard, board?.workspaceId ?? ""),
      );
    },
  }),

  // ─── POST /cards/:id/move ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, { listId?: string; position?: number }>({
    id: "cards.move",
    method: "POST",
    route: "/cards/:id/move",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as { listId?: string; position?: number };
      if (!data.listId) {
        return {
          status: 400,
          body: { message: "listId required", code: "BAD_REQUEST" },
        };
      }
      const moved = store.moveCard(params.id, data.listId, data.position ?? 0);
      if (!moved) return notFound("Card not found");
      return ok({
        id: params.id,
        listId: data.listId,
        position: data.position ?? 0,
      });
    },
  }),
];
