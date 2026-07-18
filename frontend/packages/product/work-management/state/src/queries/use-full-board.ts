import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/query"
import { boardApi } from "../api/board.api"

export function useFullBoard(boardId?: string, workspaceId?: string) {
  const query = useQuery({
    queryKey: queryKeys.boards.fullBoard(boardId ?? "pending", workspaceId),
    queryFn: () => boardApi.getFullBoard(boardId!, { workspaceId: workspaceId! }),
    enabled: Boolean(boardId && workspaceId),
    staleTime: 10_000,
  })

  return {
    board: query.data?.board,
    groups: query.data?.groups ?? [],
    fieldDefinitions: query.data?.fieldDefinitions ?? [],
    isLoading: query.isLoading,
    error: query.error,
  }
}
