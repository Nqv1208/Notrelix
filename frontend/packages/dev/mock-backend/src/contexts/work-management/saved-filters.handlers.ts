/**
 * Work Management — the saved filters of a board (the filter bar's "Bộ lọc đã lưu" menu).
 *
 * Operations:
 *   boards.savedFilters.list       — GET    /boards/:boardId/saved-filters
 *   boards.savedFilters.create     — POST   /boards/:boardId/saved-filters
 *   boards.savedFilters.rename     — PATCH  /saved-filters/:filterId/rename
 *   boards.savedFilters.visibility — PATCH  /saved-filters/:filterId/visibility
 *   boards.savedFilters.delete     — DELETE /saved-filters/:filterId
 *   boards.savedFilters.restore    — POST   /saved-filters/:filterId/restore
 *
 * The rules mirror the producer: a saved filter is a Board child decided on its
 * Board (an Editor saves and changes), a personal filter is its creator's alone
 * and another member is answered as if it did not exist, a shared one follows
 * the Board, and only the creator changes who sees it.
 */

import { defineMockOperation } from "../../operations/types";
import {
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import type { MockSavedFilter } from "../../state/mock-board-saved-filters";
import type { MockStore } from "../../state/mock-store";
import { preconditionFailed } from "../workspace/space-target";
import { canEditBoard, canReadBoard } from "./boards.handlers";

function dto(filter: MockSavedFilter, me: string) {
  return {
    id: filter.id,
    name: filter.name,
    boardId: filter.boardId,
    viewId: null,
    visibility: filter.visibility,
    createdAt: filter.createdAt,
    criteria: filter.criteria,
    isMine: filter.createdBy === me,
    version: filter.version,
  };
}

/** The filter the caller may change, or the answer that refuses the write. */
function editable(store: MockStore, filterId: string, includeDeleted = false) {
  const filter = store.savedFilters.find(filterId);
  const me = store.getCurrentUser().id;
  if (
    !filter ||
    (filter.deleted && !includeDeleted) ||
    !canReadBoard(filter.boardId, store) ||
    (filter.visibility === "Private" && filter.createdBy !== me)
  )
    return {
      filter: null,
      refusal: notFound("Saved filter not found"),
    } as const;
  if (!canEditBoard(filter.boardId, store))
    return { filter: null, refusal: forbidden() } as const;
  return { filter, refusal: null } as const;
}

type BoardParams = { boardId: string };
type FilterParams = { filterId: string };

export const savedFiltersOperations = [
  defineMockOperation<BoardParams, never, unknown[]>({
    id: "boards.savedFilters.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.SavedFilters.List",
    },
    method: "GET",
    route: "/boards/:boardId/saved-filters",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      const me = store.getCurrentUser().id;
      return ok(
        store.savedFilters.list(params.boardId, me).map((f) => dto(f, me)),
      );
    },
  }),

  defineMockOperation<
    BoardParams,
    { name?: string; criteria?: string | null; visibility?: number },
    unknown
  >({
    id: "boards.savedFilters.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.SavedFilters.Create",
    },
    method: "POST",
    route: "/boards/:boardId/saved-filters",
    async handle({ params, body, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      if (!canEditBoard(params.boardId, store)) return forbidden();
      const name = (body?.name ?? "").trim();
      if (name === "") return validationError({ name: ["Name the filter."] });
      const filter = store.savedFilters.add({
        boardId: params.boardId,
        name,
        criteria: body?.criteria ?? null,
        createdBy: store.getCurrentUser().id,
      });
      if (body?.visibility === 1) filter.visibility = "Public";
      return created(filter.id);
    },
  }),

  defineMockOperation<
    FilterParams,
    { name?: string; expectedVersion?: number },
    void
  >({
    id: "boards.savedFilters.rename",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.SavedFilters.Rename",
    },
    method: "PATCH",
    route: "/saved-filters/:filterId/rename",
    async handle({ params, body, store }) {
      const { filter, refusal } = editable(store, params.filterId);
      if (!filter) return refusal;
      const name = (body?.name ?? "").trim();
      if (name === "") return validationError({ name: ["Name the filter."] });
      if (body?.expectedVersion !== filter.version) return preconditionFailed();
      filter.name = name;
      filter.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<
    FilterParams,
    { visibility?: number; expectedVersion?: number },
    void
  >({
    id: "boards.savedFilters.visibility",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.SavedFilters.UpdateVisibility",
    },
    method: "PATCH",
    route: "/saved-filters/:filterId/visibility",
    async handle({ params, body, store }) {
      const { filter, refusal } = editable(store, params.filterId);
      if (!filter) return refusal;
      if (filter.createdBy !== store.getCurrentUser().id) return forbidden();
      if (body?.visibility !== 0 && body?.visibility !== 1)
        return validationError({ visibility: ["Unknown visibility."] });
      if (body.expectedVersion !== filter.version) return preconditionFailed();
      filter.visibility = body.visibility === 1 ? "Public" : "Private";
      filter.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FilterParams, never, void>({
    id: "boards.savedFilters.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.SavedFilters.SoftDelete",
    },
    method: "DELETE",
    route: "/saved-filters/:filterId",
    async handle({ params, store, query }) {
      const { filter, refusal } = editable(store, params.filterId);
      if (!filter) return refusal;
      if (Number(query.expectedVersion) !== filter.version)
        return preconditionFailed();
      filter.deleted = true;
      filter.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FilterParams, never, void>({
    id: "boards.savedFilters.restore",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.SavedFilters.Restore",
    },
    method: "POST",
    route: "/saved-filters/:filterId/restore",
    async handle({ params, store }) {
      const { filter, refusal } = editable(store, params.filterId, true);
      if (!filter) return refusal;
      filter.deleted = false;
      filter.version += 1;
      return noContent();
    },
  }),
];
