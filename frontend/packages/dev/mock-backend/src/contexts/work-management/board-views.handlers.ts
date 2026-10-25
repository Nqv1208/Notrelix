/**
 * Work Management — saved views of a board (the view tabs and the settings'
 * Views section).
 *
 * Operations:
 *   boards.views.list         — GET    /boards/:boardId/views/saved
 *   boards.views.create       — POST   /boards/:boardId/views
 *   boards.views.updateConfig — PATCH  /boards/:boardId/views/:viewId
 *   boards.views.rename       — PATCH  /boards/:boardId/views/:viewId/rename
 *   boards.views.visibility   — PATCH  /boards/:boardId/views/:viewId/visibility
 *   boards.views.setDefault   — POST   /boards/:boardId/views/:viewId/set-default
 *   boards.views.delete       — DELETE /boards/:boardId/views/:viewId
 *
 * The rules mirror the producer: a shared view is every member's and an
 * Editor changes it; a private view is its creator's alone, and another member
 * is answered as if it did not exist; only the creator changes who sees a view.
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
  savedViewTypeName,
  type MockSavedView,
} from "../../state/mock-board-saved-views";
import type { MockStore } from "../../state/mock-store";
import {
  canEditBoard,
  canReadBoard,
  keepViewConfig,
  readKeptViewConfig,
} from "./boards.handlers";

function dto(view: MockSavedView) {
  const kept = view.id.endsWith("-view-default")
    ? readKeptViewConfig(view.boardId)
    : null;
  return {
    id: view.id,
    boardId: view.boardId,
    name: view.name,
    type: view.type,
    config: kept ?? view.config,
    isDefault: view.isDefault,
    visibility: view.visibility,
    createdBy: view.createdBy,
    version: view.version,
  };
}

/** The view the caller may change, or the answer that refuses the write. */
function editable(store: MockStore, boardId: string, viewId: string) {
  if (!canReadBoard(boardId, store))
    return { view: null, refusal: notFound("Board not found") } as const;
  const view = store.savedViews.find(boardId, viewId);
  const me = store.getCurrentUser().id;
  if (!view || (view.visibility === "Private" && view.createdBy !== me))
    return { view: null, refusal: notFound("View not found") } as const;
  if (view.visibility === "Board" && !canEditBoard(boardId, store))
    return { view: null, refusal: forbidden() } as const;
  return { view, refusal: null } as const;
}

type BoardParams = { boardId: string };
type ViewParams = BoardParams & { viewId: string };

export const boardViewsOperations = [
  defineMockOperation<BoardParams, never, unknown[]>({
    id: "boards.views.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.List",
    },
    method: "GET",
    route: "/boards/:boardId/views/saved",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      const me = store.getCurrentUser().id;
      return ok(store.savedViews.list(params.boardId, me).map(dto));
    },
  }),

  defineMockOperation<
    BoardParams,
    { name?: string; viewMode?: string; config?: string; visibility?: string },
    unknown
  >({
    id: "boards.views.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Create",
    },
    method: "POST",
    route: "/boards/:boardId/views",
    async handle({ params, body, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      if (!canEditBoard(params.boardId, store)) return forbidden();
      const type = savedViewTypeName(body?.viewMode);
      if (!type) return validationError({ viewMode: ["Unknown view mode."] });
      const name = (body?.name ?? "").trim();
      if (name === "") return validationError({ name: ["Name the view."] });
      const visibility = body?.visibility === "Private" ? "Private" : "Board";
      const created = store.savedViews.add({
        boardId: params.boardId,
        name,
        type,
        config: body?.config ?? "{}",
        visibility,
        createdBy: store.getCurrentUser().id,
      });
      return ok(dto(created));
    },
  }),

  defineMockOperation<ViewParams, { configJson?: string }, unknown>({
    id: "boards.views.updateConfig",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.UpdateConfig",
    },
    method: "PATCH",
    route: "/boards/:boardId/views/:viewId",
    async handle({ params, body, store }) {
      const { view, refusal } = editable(store, params.boardId, params.viewId);
      if (!view) return refusal;
      view.config = body?.configJson ?? "{}";
      view.version += 1;
      if (view.id.endsWith("-view-default"))
        keepViewConfig(view.boardId, view.config);
      return ok(dto(view));
    },
  }),

  defineMockOperation<ViewParams, { name?: string }, void>({
    id: "boards.views.rename",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Rename",
    },
    method: "PATCH",
    route: "/boards/:boardId/views/:viewId/rename",
    async handle({ params, body, store }) {
      const { view, refusal } = editable(store, params.boardId, params.viewId);
      if (!view) return refusal;
      const name = (body?.name ?? "").trim();
      if (name === "") return validationError({ name: ["Name the view."] });
      view.name = name;
      view.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<ViewParams, { visibility?: string }, void>({
    id: "boards.views.visibility",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.SetVisibility",
    },
    method: "PATCH",
    route: "/boards/:boardId/views/:viewId/visibility",
    async handle({ params, body, store }) {
      const { view, refusal } = editable(store, params.boardId, params.viewId);
      if (!view) return refusal;
      if (view.createdBy !== store.getCurrentUser().id)
        return validationError({
          visibility: ["Only the person who made a view changes who sees it."],
        });
      if (body?.visibility !== "Board" && body?.visibility !== "Private")
        return validationError({ visibility: ["Board or Private."] });
      view.visibility = body.visibility;
      view.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<ViewParams, never, void>({
    id: "boards.views.setDefault",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.SetDefault",
    },
    method: "POST",
    route: "/boards/:boardId/views/:viewId/set-default",
    async handle({ params, store }) {
      const { view, refusal } = editable(store, params.boardId, params.viewId);
      if (!view) return refusal;
      if (view.visibility === "Private")
        return validationError({
          view: ["A private view cannot be the Board's default view."],
        });
      store.savedViews.setDefault(params.boardId, view.id);
      return noContent();
    },
  }),

  defineMockOperation<ViewParams, never, void>({
    id: "boards.views.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Delete",
    },
    method: "DELETE",
    route: "/boards/:boardId/views/:viewId",
    async handle({ params, store }) {
      const { view, refusal } = editable(store, params.boardId, params.viewId);
      if (!view) return refusal;
      store.savedViews.remove(params.boardId, view.id);
      return noContent();
    },
  }),
];
