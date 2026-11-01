import type {
  NotrelixClient,
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import {
  parseSavedFilterCriteria,
  serializeSavedFilterCriteria,
  type SavedFilter,
  type SavedFilterCriteria,
} from "@notrelix/work-management-core";

type ListSavedFiltersResponse = OperationResponse<
  "WorkManagement.SavedFilters.List",
  200
>;
type CreateSavedFilterBody =
  OperationRequestBody<"WorkManagement.SavedFilters.Create">;
type CreateSavedFilterResponse = OperationResponse<
  "WorkManagement.SavedFilters.Create",
  201
>;
type RenameSavedFilterBody =
  OperationRequestBody<"WorkManagement.SavedFilters.Rename">;
type VisibilityBody =
  OperationRequestBody<"WorkManagement.SavedFilters.UpdateVisibility">;

export function createSavedFilterApi(client: NotrelixClient) {
  const api = client.api;
  return {
    /** The filters the signed-in member sees on a board: theirs and the shared ones. */
    async list(boardId: string): Promise<SavedFilter[]> {
      const rows = await api.get<ListSavedFiltersResponse>(
        endpoints.savedFilters.forBoard(boardId),
      );
      return (rows ?? []).flatMap((row) =>
        row.id
          ? [
              {
                id: row.id,
                boardId: row.boardId ?? boardId,
                name: row.name ?? "",
                visibility: row.visibility === "Public" ? "board" : "me",
                isMine: row.isMine === true,
                criteria: parseSavedFilterCriteria(row.criteria),
                version: row.version ?? 0,
              } satisfies SavedFilter,
            ]
          : [],
      );
    },

    /** Saves the filter bar as a new filter (personal or shared); resolves with its id. */
    async create(
      boardId: string,
      input: {
        name: string;
        criteria: SavedFilterCriteria;
        visibility: "board" | "me";
      },
    ): Promise<string> {
      const body: CreateSavedFilterBody = {
        name: input.name,
        criteria: serializeSavedFilterCriteria(input.criteria),
        // The producer's enum on the wire: 0 Private, 1 Public.
        visibility: input.visibility === "board" ? 1 : 0,
      };
      return api.post<CreateSavedFilterResponse>(
        endpoints.savedFilters.forBoard(boardId),
        body,
      );
    },

    async rename(
      filterId: string,
      name: string,
      expectedVersion: number,
    ): Promise<void> {
      const body: RenameSavedFilterBody = { name, expectedVersion };
      await api.patch<void>(endpoints.savedFilters.rename(filterId), body);
    },

    async setVisibility(
      filterId: string,
      visibility: "board" | "me",
      expectedVersion: number,
    ): Promise<void> {
      const body: VisibilityBody = {
        // The producer's enum on the wire: 0 Private, 1 Public.
        visibility: visibility === "board" ? 1 : 0,
        expectedVersion,
      };
      await api.patch<void>(endpoints.savedFilters.visibility(filterId), body);
    },

    async remove(filterId: string, expectedVersion: number): Promise<void> {
      await api.delete<void>(
        `${endpoints.savedFilters.detail(filterId)}?expectedVersion=${expectedVersion}`,
      );
    },

    async restore(filterId: string): Promise<void> {
      await api.post<void>(endpoints.savedFilters.restore(filterId), {});
    },
  };
}
