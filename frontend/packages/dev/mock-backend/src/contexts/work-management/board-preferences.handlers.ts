/**
 * Work Management — a member's personal state of one view of a board.
 *
 *   boards.preferences.get    — GET /boards/:boardId/preferences/:viewId
 *   boards.preferences.update — PUT /boards/:boardId/preferences/:viewId
 *
 * The rules mirror the producer: anyone who can view the board reads and
 * writes their own (viewers included); the criteria is an opaque JSON object
 * of at most 8 KB (UTF-8); blank clears it; omitting it leaves it unchanged.
 */

import { defineMockOperation } from "../../operations/types";
import {
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import { canReadBoard } from "./boards.handlers";

type Params = { boardId: string; viewId: string };

const MAX_CRITERIA_BYTES = 8192;

function isJsonObject(text: string): boolean {
  try {
    const value: unknown = JSON.parse(text);
    return typeof value === "object" && value !== null && !Array.isArray(value);
  } catch {
    return false;
  }
}

export const boardPreferencesOperations = [
  defineMockOperation<Params, never, unknown>({
    id: "boards.preferences.get",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardPreferences.Get",
    },
    method: "GET",
    route: "/boards/:boardId/preferences/:viewId",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      const me = store.getCurrentUser().id;
      return ok({
        id: params.viewId,
        boardId: params.boardId,
        viewId: params.viewId,
        filterRules: [],
        sortRules: [],
        groupRule: null,
        criteria: store.boardPreferences.get(me, params.boardId, params.viewId),
      });
    },
  }),

  defineMockOperation<Params, { criteria?: string | null }, void>({
    id: "boards.preferences.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardPreferences.CreateOrUpdate",
    },
    method: "PUT",
    route: "/boards/:boardId/preferences/:viewId",
    async handle({ params, body, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      const criteria = body?.criteria;
      if (typeof criteria === "string" && criteria.trim() !== "") {
        if (new TextEncoder().encode(criteria).length > MAX_CRITERIA_BYTES)
          return validationError({
            criteria: ["Criteria must be at most 8 KB."],
          });
        if (!isJsonObject(criteria))
          return validationError({
            criteria: ["Criteria must be a JSON object."],
          });
      }
      if (typeof criteria === "string")
        store.boardPreferences.set(
          store.getCurrentUser().id,
          params.boardId,
          params.viewId,
          criteria,
        );
      return noContent();
    },
  }),
];
