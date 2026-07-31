import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useWorkspaceBoards(workspaceId: string) {
  const { boards } = useWorkManagementServices()
  return useQuery({
    queryKey: queryKeys.boards.workspaceList(workspaceId),
    queryFn: () => boards.getBoardsByWorkspaceId(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
