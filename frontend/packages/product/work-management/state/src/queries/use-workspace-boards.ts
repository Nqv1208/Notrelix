import { useQuery } from "@tanstack/react-query"
import { wmQueryKeys } from "./keys"
import { useWorkManagementServices } from "../services"

export function useWorkspaceBoards(workspaceId: string) {
  const { boards } = useWorkManagementServices()
  return useQuery({
    queryKey: wmQueryKeys.workspaceList(workspaceId),
    queryFn: () => boards.getBoardsByWorkspaceId(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
