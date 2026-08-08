import { useQuery } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useFullBoard(boardId?: string, workspaceId?: string) {
  const { boards } = useWorkManagementServices()
  const query = useQuery({
    queryKey: wmQueryKeys.fullBoard(boardId ?? "pending", workspaceId),
    queryFn: () => boards.getFullBoard(boardId!, { workspaceId: workspaceId! }),
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
