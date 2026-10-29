import type {
  NotrelixClient,
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import {
  parseSavedFilterCriteria,
  type SavedFilterCriteria,
} from "@notrelix/work-management-core";

type PreferenceResponse = OperationResponse<
  "WorkManagement.BoardPreferences.Get",
  200
>;
type PreferenceBody =
  OperationRequestBody<"WorkManagement.BoardPreferences.CreateOrUpdate">;

/** A member's personal state of one view: the opaque criteria JSON, parsed like a saved filter's. */
export function createBoardPreferenceApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async get(boardId: string, viewId: string): Promise<SavedFilterCriteria> {
      const row = await api.get<PreferenceResponse>(
        endpoints.boardPreferences.forView(boardId, viewId),
      );
      return parseSavedFilterCriteria(row?.criteria);
    },

    /** Replaces the criteria; an empty string clears them (the typed filter, sort and group rules are left alone). */
    async saveCriteria(
      boardId: string,
      viewId: string,
      criteriaJson: string,
    ): Promise<void> {
      const body: PreferenceBody = { criteria: criteriaJson };
      await api.put<void>(
        endpoints.boardPreferences.forView(boardId, viewId),
        body,
      );
    },
  };
}
