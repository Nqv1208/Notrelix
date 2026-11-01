import type {
  NotrelixClient,
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import {
  parseSavedFilterCriteria,
  parseTableColumnPreference,
  type SavedFilterCriteria,
  type TableColumnPreference,
} from "@notrelix/work-management-core";

type PreferenceResponse = OperationResponse<
  "WorkManagement.BoardPreferences.Get",
  200
>;
type PreferenceBody =
  OperationRequestBody<"WorkManagement.BoardPreferences.CreateOrUpdate">;

/** What a member keeps of one view: the personal view state and the table columns. */
export interface BoardPreferenceState {
  readonly criteria: SavedFilterCriteria;
  readonly columns: TableColumnPreference;
}

/** A member's personal state of one view: the opaque criteria and columns JSON, parsed. */
export function createBoardPreferenceApi(client: NotrelixClient) {
  const api = client.api;
  // The first write creates the row, so two writes of one view never overlap.
  const queues = new Map<string, Promise<unknown>>();
  const send = (
    boardId: string,
    viewId: string,
    body: PreferenceBody,
  ): Promise<void> => {
    const key = `${boardId}:${viewId}`;
    const write = () =>
      api.put<void>(endpoints.boardPreferences.forView(boardId, viewId), body);
    const next = (queues.get(key) ?? Promise.resolve()).then(write, write);
    queues.set(
      key,
      next.catch(() => undefined),
    );
    return next;
  };

  return {
    async get(boardId: string, viewId: string): Promise<BoardPreferenceState> {
      const row = await api.get<PreferenceResponse>(
        endpoints.boardPreferences.forView(boardId, viewId),
      );
      return {
        criteria: parseSavedFilterCriteria(row?.criteria),
        columns: parseTableColumnPreference(row?.columns),
      };
    },

    /** Replaces the criteria; an empty string clears them (the typed filter, sort and group rules are left alone). */
    saveCriteria(
      boardId: string,
      viewId: string,
      criteriaJson: string,
    ): Promise<void> {
      return send(boardId, viewId, { criteria: criteriaJson });
    },

    /** Replaces the table columns; an empty string clears them. */
    saveColumns(
      boardId: string,
      viewId: string,
      columnsJson: string,
    ): Promise<void> {
      return send(boardId, viewId, { columns: columnsJson });
    },
  };
}
