import { useQuery, useQueryClient } from "@tanstack/react-query";
import { readBoard } from "./board-read";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

export function useFullBoard(boardId?: string, workspaceId?: string) {
  const services = useWorkManagementServices();
  const queryClient = useQueryClient();
  const resolvedBoardId = boardId ?? "pending";
  const resolvedWorkspaceId = workspaceId ?? "pending";
  const query = useQuery({
    queryKey: wmQueryKeys.fullBoard(resolvedWorkspaceId, resolvedBoardId),
    queryFn: () => {
      if (!boardId || !workspaceId) {
        throw new Error("boardId and workspaceId are required to load a board");
      }
      return readBoard(services, queryClient, workspaceId, boardId);
    },
    enabled: Boolean(boardId && workspaceId),
    staleTime: 10_000,
  });

  // A refused or vanished board shows nothing of the data read before: a person who lost access
  // sees the generic state, never a name or a card (the answer is the same NotFound for both).
  const kind = (query.error as { kind?: string } | null)?.kind;
  const gone = kind === "not_found" || kind === "forbidden";
  const data = gone ? undefined : query.data;

  return {
    board: data?.board,
    groups: data?.groups ?? [],
    archivedGroupIds: data?.archivedGroupIds ?? [],
    fieldDefinitions: data?.fieldDefinitions ?? [],
    /** Present when the groups hold only the first pages of each Status lane. */
    paging: data?.paging,
    isLoading: query.isLoading,
    error: query.error,
    refetch: query.refetch,
  };
}
