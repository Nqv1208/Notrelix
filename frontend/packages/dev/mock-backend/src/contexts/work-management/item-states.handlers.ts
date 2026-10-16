/**
 * Work Management — what the States mockup adds around an item (gap
 * `CTR-GAP-WM-ITEM-STATES`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: States"):
 *
 *   itemStates.lifecycle    — GET  /board-items/:id/lifecycle
 *   itemStates.restore      — POST /board-items/:id/restore
 *   itemStates.people       — GET  /boards/:boardId/people-directory
 *   itemStates.invitePerson — POST /boards/:boardId/people-directory/invitations
 */
import { defineMockOperation } from "../../operations/types";
import {
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  peopleDirectory,
  type PeopleDirectoryDto,
} from "../../state/mock-people-directory";
import {
  canManageBoard,
  canReadBoard,
  canWriteBoardItems,
} from "./boards.handlers";

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

  defineMockOperation<{ id: string }, never, void>({
    id: "itemStates.restore",
    contract: gap,
    method: "POST",
    route: "/board-items/:id/restore",
    async handle({ params, store, request }) {
      if (!request.headers.get("Idempotency-Key"))
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      const item = store.getCard(params.id);
      if (!item) return notFound();
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      store.restoreCard(params.id);
      return noContent();
    },
  }),

  defineMockOperation<{ boardId: string }, never, PeopleDirectoryDto>({
    id: "itemStates.people",
    contract: gap,
    method: "GET",
    route: "/boards/:boardId/people-directory",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store)) return notFound();
      const directory = peopleDirectory(
        store,
        params.boardId,
        canManageBoard(params.boardId, store),
      );
      return directory ? ok(directory) : notFound();
    },
  }),

  defineMockOperation<{ boardId: string }, { userId?: string }, void>({
    id: "itemStates.invitePerson",
    contract: gap,
    method: "POST",
    route: "/boards/:boardId/people-directory/invitations",
    async handle({ params, body, store, request }) {
      if (!request.headers.get("Idempotency-Key"))
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      if (!canReadBoard(params.boardId, store)) return notFound();
      if (!canManageBoard(params.boardId, store)) return forbidden();
      const userId = body?.userId;
      if (!userId || !store.getUser(userId))
        return validationError({ userId: ["Choose a person."] });
      store.addBoardMember(params.boardId, userId);
      return noContent();
    },
  }),
];
