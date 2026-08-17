/**
 * Work Management — Boards context handlers.
 *
 * Operations:
 *   boards.listByWorkspace — GET /workspaces/:workspaceId/boards
 *   boards.detail          — GET /boards/:id
 *   boards.full            — GET /boards/:id/full
 *   boards.create          — POST /workspaces/:workspaceId/boards
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 * "Do not keep growing one monolithic handler file."
 */

import type {
  BoardDtoApi,
  FullBoardDtoApi,
  CardSummaryDtoApi,
  ListDtoApi,
} from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";

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
        })),
      );
    },
  }),

  // ─── GET /boards/:id ─────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BoardDtoApi>({
    id: "boards.detail",
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
    BoardDtoApi
  >({
    id: "boards.create",
    method: "POST",
    route: "/workspaces/:workspaceId/boards",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as { title?: string; description?: string };
      const newBoard = store.createBoard(params.workspaceId, {
        title: data.title,
        description: data.description,
      });
      return created<BoardDtoApi>({
        id: newBoard.id,
        workspaceId: newBoard.workspaceId,
        title: newBoard.title,
        description: newBoard.description ?? null,
        background: JSON.stringify(newBoard.background),
        visibility: newBoard.visibility,
        isArchived: newBoard.isArchived,
        memberCount: 1,
        listCount: 0,
        createdAt: newBoard.createdAt,
      });
    },
  }),
];
