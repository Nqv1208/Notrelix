import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { boardApi } from "../api/board.api"

export function useWorkspaceBoards(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.boards.workspaceList(workspaceId),
    queryFn: () => boardApi.getBoardsByWorkspaceId(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
