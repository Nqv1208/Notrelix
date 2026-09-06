import { useQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

export function useFullBoard(boardId?: string, workspaceId?: string) {
  const { boards } = useWorkManagementServices();
  const resolvedBoardId = boardId ?? "pending";
  const resolvedWorkspaceId = workspaceId ?? "pending";
  const query = useQuery({
    queryKey: wmQueryKeys.fullBoard(resolvedWorkspaceId, resolvedBoardId),
    queryFn: () => {
      if (!boardId || !workspaceId) {
        throw new Error("boardId and workspaceId are required to load a board");
      }
      return boards.getFullBoard(boardId, { workspaceId });
    },
    enabled: Boolean(boardId && workspaceId),
    staleTime: 10_000,
  });

  return {
    board: query.data?.board,
    groups: query.data?.groups ?? [],
    fieldDefinitions: query.data?.fieldDefinitions ?? [],
    isLoading: query.isLoading,
    error: query.error,
  };
}
